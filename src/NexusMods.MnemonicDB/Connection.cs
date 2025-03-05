using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using DynamicData;
using Jamarino.IntervalTree;
using Microsoft.Extensions.Logging;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.DatomIterators;
using NexusMods.MnemonicDB.Abstractions.Query;
using NexusMods.MnemonicDB.EventTypes;
using NexusMods.MnemonicDB.InternalTxFunctions;
using NexusMods.MnemonicDB.Storage;
using R3;
using Observable = System.Reactive.Linq.Observable;

namespace NexusMods.MnemonicDB;

using DatomChangeSet = ChangeSet<Datom, DatomKey, IDb>;

/// <summary>
///     Main connection class, co-ordinates writes and immutable reads
/// </summary>
public class Connection : IConnection
{
    private readonly DatomStore _store;
    private readonly ILogger<Connection> _logger;

    private readonly DbStream _dbStream;
    private IDisposable? _dbStreamDisposable;
    private readonly IAnalyzer[] _analyzers;
    
    /// <summary>
    /// A tree of observers that are interested in datoms of a given range
    /// </summary>
    private readonly LightIntervalTree<Datom, IObserver<DatomChangeSet>> _datomObservers = new();
    
    // Temporary storage for observers that are being processed
    private readonly Dictionary<IObserver<DatomChangeSet>, DatomChangeSet> _changeSets = new();
    private readonly List<Change<Datom, DatomKey>> _localChanges = [];
    
    // Pending events that need to be processed
    private readonly Channel<IEvent> _pendingEvents = Channel.CreateUnbounded<IEvent>();
    
    //private readonly BlockingCollection<IEvent> _pendingEvents = new(new ConcurrentQueue<IEvent>());
    private Thread _eventThread = null!;

    private static readonly IndexType[] IndexTypes =
    [
        IndexType.EAVTCurrent, IndexType.AEVTCurrent, IndexType.VAETCurrent, IndexType.AVETCurrent,
        IndexType.TxLog
    ];

    /// <summary>
    ///     Main connection class, co-ordinates writes and immutable reads
    /// </summary>
    public Connection(ILogger<Connection> logger, IDatomStore store, IServiceProvider provider, IEnumerable<IAnalyzer> analyzers, bool readOnlyMode = false)
    {
        ServiceProvider = provider;
        AttributeCache = store.AttributeCache;
        AttributeResolver = new AttributeResolver(provider, AttributeCache);
        _logger = logger;
        _store = (DatomStore)store;
        _dbStream = new DbStream();
        _analyzers = analyzers.ToArray();
        Bootstrap(readOnlyMode);
    }

    private void ProcessEvents()
    {
        List<IEvent> events = new();
        try
        {
            while (true)
            {
                IEvent action;
                var task = _pendingEvents.Reader.ReadAsync();
                action = task.IsCompleted ? task.Result : task.AsTask().Result;
                events.Clear();
                events.Add(action);
                
                // We do some event compression here. We try to get as many events of the same type as possible, then
                // process them all at once. For subscriptions this means we can prime them in parallel, and for unsubscribes
                // we can remove them all at once, reducing overhead of some of the more expensive memoized data structures.
                AddWhileOfSameType(events, _pendingEvents, action.GetType());

                try
                {
                    switch (action)
                    {
                        // Observers are done and want to be removed
                        case UnSubscribeEvent:
                            UnsubscribeAll(events);
                            break;
                        // Observers that want to be added
                        case IObserveDatomsEvent:
                            ObserveAll(events);
                            break;
                        // A new DB revision
                        case NewRevisionEvent:
                        {
                            foreach (var newEvent in events.Cast<NewRevisionEvent>())
                            {
                                ProcessNewRevision(newEvent);
                            }
                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to process {Count} events of type {Event}", events.Count, action.GetType().Name);
                }
                
            }
        }
        catch (AggregateException ex)
        {
            if (ex.InnerExceptions[0] is not ChannelClosedException)
            {
                _logger.LogError(ex, "Failed to process events");
            }
        }
    }

    private ConcurrentBag<(Datom, Datom, IObserver<DatomChangeSet>)> _pendingObservers = [];

    private void ProcessNewRevision(NewRevisionEvent newEvent)
    {
        foreach (var analyzer in _analyzers)
        {
            try
            {
                var result = analyzer.Analyze(newEvent.Prev, newEvent.Db);
                newEvent.Db.AddAnalyzerData(analyzer.GetType(), result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to analyze with {Analyzer}", analyzer.GetType().Name);
            }
        }

        _dbStream.OnNext(newEvent.Db);
        ProcessObservers(newEvent.Db);
        newEvent.OnFinished.SetResult();
    }
    private void ObserveAll(List<IEvent> events)
    {
        const int maxObserversBeforeParallel = 32;
        if (events.Count <= maxObserversBeforeParallel)
        {
            foreach (var action in events.Cast<IObserveDatomsEvent>())
            {
                var (from, to, observer) = action.Prime(Db);
                _datomObservers.Add(from, to, observer);
            }
        }
        else
        {
            _pendingObservers.Clear();
            Parallel.ForEach(events.Cast<IObserveDatomsEvent>(), action =>
            {
                var (from, to, observer) = action.Prime(Db);
                _pendingObservers.Add((from, to, observer));
            });
            foreach (var (from, to, observer) in _pendingObservers)
            {
                _datomObservers.Add(from, to, observer);
            }
            _pendingObservers.Clear();
        }
        
    }

    private void UnsubscribeAll(List<IEvent> action)
    {
        var observers = action.Cast<UnSubscribeEvent>().Select(s => s.Observer).ToHashSet();
        _datomObservers.RemoveWhere(static (observer, set) => set.Contains(observer), observers);
    }

    /// <summary>
    /// Gets all the items possible from the blocking collection that are of the same type as the first item, and puts
    /// them into the list.
    /// </summary>
    private void AddWhileOfSameType(List<IEvent> events, Channel<IEvent> pendingEvents, Type getType)
    {
        while (true)
        {
            if (!pendingEvents.Reader.TryPeek(out var action))
                return;
            if (action.GetType() != getType)
            {
                return;
            }
            pendingEvents.Reader.TryRead(out action);
            events.Add(action!);
        }
        
    }

    /// <summary>
    /// Scrubs the transaction stream so that we only ever move forward and never repeat transactions
    /// </summary>
    private IDisposable ProcessUpdates(IObservable<IDb> dbStream)
    {
        IDb? prev = null;
        
        return dbStream
            .Subscribe(idb =>
        {
            var db = idb;
            db.Connection = this;
            var tcs = new TaskCompletionSource();
            _pendingEvents.Writer.TryWrite(new NewRevisionEvent(prev, db, tcs));
            prev = db;
            tcs.Task.Wait();
        });
    }

    private void ProcessObservers(IDb db)
    {
        var recentlyAdded = db.RecentlyAdded;
        var cache = db.AttributeCache;
        _changeSets.Clear();
        
        // For each recently added datom, we need to find listeners to it
        for (var i = 0; i < recentlyAdded.Count; i++)
        {
            // We're going to add changes to this list, and then send them all at the end
            _localChanges.Clear();
                
            var datom = recentlyAdded[i];
                
            var isMany = cache.IsCardinalityMany(datom.A);
            var attr = datom.A;
            if (datom.IsRetract)
            {
                    
                // If the attribute is cardinality many, we can just remove the datom
                if (isMany)
                {
                    _localChanges.Add(new Change<Datom, DatomKey>(ChangeReason.Remove, CreateKey(datom, attr, true), datom));
                    goto PROCESS_CHANGES;
                }
                    
                // If at the end of the segment
                if (i + 1 >= recentlyAdded.Count)
                {
                    _localChanges.Add(new Change<Datom, DatomKey>(ChangeReason.Remove, CreateKey(datom, attr), datom));
                    goto PROCESS_CHANGES;
                }

                // If the next datom is not the same E or A, we can remove the datom
                var nextDatom = recentlyAdded[i + 1];
                if (nextDatom.E != datom.E || nextDatom.A != datom.A)
                {
                    _localChanges.Add(new Change<Datom, DatomKey>(ChangeReason.Remove, CreateKey(datom, attr), datom));
                    goto PROCESS_CHANGES;
                }

                // Otherwise we skip the add, and issue an update, and skip the add because we've already processed it
                _localChanges.Add(new Change<Datom, DatomKey>(ChangeReason.Update, CreateKey(datom, attr), nextDatom, datom));
                i++;
            }
            else
            {
                _localChanges.Add(new Change<Datom, DatomKey>(ChangeReason.Add, CreateKey(datom, attr, isMany), datom));
            }

            // Yes, I know this is a goto, but we need a way to run some code at the end of each loop after we early exit
            // from the if-tree above.
            PROCESS_CHANGES:
                
            // Now that we've found all the changes, we need to find all the observers that are interested in this datom
                
            // We could likely be cleaner about how we do this, but this will work for now
            foreach (var index in IndexTypes)
            {
                var reindex = datom.WithIndex(index);
                foreach (var overlap in _datomObservers.Query(reindex))
                {
                    ref var changeSet = ref CollectionsMarshal.GetValueRefOrAddDefault(_changeSets, overlap, out _);
                    changeSet ??= new DatomChangeSet(db);
                    changeSet.AddRange(_localChanges);
                }
            }
        }
        
        // Release all the sends
        foreach (var (subject, changeSet) in _changeSets)
        {
            subject.OnNext(changeSet);
        }
    }
    
    /// <inheritdoc />
    public IServiceProvider ServiceProvider { get; set; }

    /// <inheritdoc />
    public IDatomStore DatomStore => _store;

    /// <inheritdoc />
    public IDb Db
    {
        get
        {
            var val = _dbStream;
            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            if (val == null)
                ThrowNullDb();
            return _dbStream.Current;
        }
    }

    /// <inheritdoc />
    public AttributeResolver AttributeResolver { get; }

    /// <inheritdoc />
    public AttributeCache AttributeCache { get; }

    private static void ThrowNullDb()
    {
        throw new InvalidOperationException("Connection not started, did you forget to start the hosted service?");
    }


    /// <inheritdoc />
    public TxId TxId => _store.AsOfTxId;

    /// <inheritdoc />
    public IDb AsOf(TxId txId)
    {
        var snapshot = new AsOfSnapshot(_store.GetSnapshot(), txId, AttributeCache);
        return snapshot.MakeDb(txId, AttributeCache, this);
    }

    /// <inheritdoc />
    public IDb History()
    {
        return new HistorySnapshot(_store.GetSnapshot(), AttributeCache).MakeDb(TxId, AttributeCache, this);
    }

    /// <inheritdoc />
    public ITransaction BeginTransaction()
    {
        return new Transaction(this);
    }

    /// <inheritdoc />
    public IAnalyzer[] Analyzers => _analyzers;


    /// <inheritdoc />
    public async Task<ICommitResult> Excise(EntityId[] entityIds)
    {
        var tx = new Transaction(this);
        tx.Set(new Excise(entityIds));
        return await tx.Commit();
    }


    /// <inheritdoc />
    public async Task<ICommitResult> ScanUpdate(IConnection.ScanFunction function)
    {
        var tx = new Transaction(this);
        tx.Set(new ScanUpdate(function));
        return await tx.Commit();
    }

    /// <inheritdoc />
    public async Task<ICommitResult> FlushAndCompact()
    {
        var tx = new Transaction(this);
        tx.Set(new FlushAndCompact());
        return await tx.Commit();
    }

    /// <inheritdoc />
    public Task UpdateSchema(params IAttribute[] attribute)
    {
        return Transact(new SimpleMigration(attribute));
    }


    public IObservable<DatomChangeSet> ObserveDatoms<TDescriptor>(TDescriptor descriptor) 
        where TDescriptor : ISliceDescriptor
    {
        return Observable.Create<DatomChangeSet>(observer =>
        {
            _pendingEvents.Writer.TryWrite(new ObserveDatomsEvent<TDescriptor>(descriptor, observer));
            
            return Disposable.Create((observer, this), static state =>
            {
                var (sObserver, self) = state;
                // Enqueue the dispose operation, we don't remove the item here, because removing a lot of items one by one
                // from the tree is expensive, so we batch them up and remove them all at once.
                self._pendingEvents.Writer.TryWrite(new UnSubscribeEvent(sObserver));
            });
        });
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static DatomKey CreateKey(Datom datom, AttributeId attrId, bool isMany = false)
    {
        return new DatomKey(datom.E, attrId, isMany ? datom.ValueMemory : Memory<byte>.Empty);
    }

    /// <inheritdoc />
    public IObservable<IDb> Revisions => _dbStream;
    
    internal async Task<ICommitResult> Transact(IInternalTxFunction fn)
    {
        StoreResult newTx;
        IDb newDb;

        (newTx, newDb) = await _store.TransactAsync(fn);
        newDb.Connection = this;
        var result = new CommitResult(newDb, newTx.Remaps);
        return result;
    }

    private void Bootstrap(bool readOnlyMode)
    {
        try
        {
            var initialSnapshot = _store.GetSnapshot();
            var initialDb = initialSnapshot.MakeDb(TxId, AttributeCache, this);
            AttributeCache.Reset(initialDb);
            
            var declaredAttributes = AttributeResolver.DefinedAttributes.OrderBy(a => a.Id.Id).ToArray();
            
            _eventThread = new Thread(ProcessEvents)
            {
                Name = "MnemonicDB: Event Thread",
                IsBackground = true
            };
            _eventThread.Start();
            
            if (!readOnlyMode)
                _store.Transact(new SimpleMigration(declaredAttributes));
            
            _dbStreamDisposable = ProcessUpdates(_store.TxLog);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add missing attributes");
        }
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken)
    {
        _dbStreamDisposable?.Dispose();
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _pendingEvents.Writer.TryComplete();
        _eventThread.Join();
    }
}
