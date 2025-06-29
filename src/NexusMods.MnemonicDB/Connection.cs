﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using DynamicData;
using Jamarino.IntervalTree;
using Microsoft.Extensions.Logging;
using NexusMods.Cascade;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Cascade;
using NexusMods.MnemonicDB.Abstractions.DatomIterators;
using NexusMods.MnemonicDB.EventTypes;
using NexusMods.MnemonicDB.InternalTxFunctions;
using NexusMods.MnemonicDB.Storage;
using NexusMods.MnemonicDB.Storage.RocksDbBackend;
using R3;
using Observable = System.Reactive.Linq.Observable;

namespace NexusMods.MnemonicDB;

using DatomChangeSet = ChangeSet<Datom, DatomKey, IDb>;

/// <summary>
///     Main connection class, co-ordinates writes and immutable reads
/// </summary>
public sealed class Connection : IConnection
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

    private readonly CancellationTokenSource _cts = new();
    private readonly BlockingCollection<IEvent> _pendingEvents = new(new ConcurrentQueue<IEvent>());
    private readonly ConcurrentBag<(Datom, Datom, IObserver<DatomChangeSet>)> _pendingObservers = [];

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
        Topology = new Topology();
        _dbInlet = Topology.Intern(Query.Db);
        
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
        List<IEvent> events = [];
        IEvent? startEvent = null;
        var startIndex = -1;
        var endIndex = -1;

        while (!_cts.IsCancellationRequested && !_pendingEvents.IsAddingCompleted)
        {
            events.Clear();

            try
            {
                var pendingEvent = _pendingEvents.Take(cancellationToken: _cts.Token);
                events.Add(pendingEvent);
            }
            catch (Exception e) when (e is OperationCanceledException or InvalidOperationException)
            {
                break;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Exception trying to take a new event");
                break;
            }

            // NOTE(erri120): taking as many items as possible without blocking for batching
            while (!_cts.IsCancellationRequested && !_pendingEvents.IsAddingCompleted)
            {
                try
                {
                    if (_pendingEvents.TryTake(out var nextEvent)) events.Add(nextEvent);
                    else break;
                }
                catch (Exception)
                {
                    break;
                }
            }

            if (_cts.IsCancellationRequested || _pendingEvents.IsAddingCompleted) return;
            Debug.Assert(events.Count > 0);

            for (var i = 0; i < events.Count; i++)
            {
                var current = events[i];

                if (startEvent is not null)
                {
                    if (startEvent.GetType() == current.GetType())
                    {
                        endIndex = i;
                        continue;
                    }

                    ProcessEventsImpl();
                    startEvent = null;
                }

                if (startEvent is not null) continue;
                startEvent = current;
                startIndex = i;
                endIndex = i;
            }

            ProcessEventsImpl();
        }

        return;
        void ProcessEventsImpl()
        {
            Debug.Assert(startEvent is not null);
            Debug.Assert(startIndex != -1);
            Debug.Assert(endIndex != -1);
            var range = new Range(start: startIndex, end: endIndex + 1);
            Debug.Assert(range.GetOffsetAndLength(events.Count).Length > 0);
   
            try
            {
                switch (startEvent)
                {
                    case IObserveDatomsEvent:
                        ObserveAll(events, range);
                        break;
                    case UnSubscribeEvent:
                        UnsubscribeAll(events, range);
                        break;
                    case NewRevisionEvent:
                        ProcessNewRevisions(events, range);
                        break;
                    default:
                        throw new Exception($"Unknown event type: {startEvent.GetType()}");
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Exception processing {Count} events", endIndex - startIndex + 1);
            }

            startEvent = null;
            startIndex = -1;
            endIndex = -1;
        }
    }

    private void ProcessNewRevisions(List<IEvent> events, Range range)
    {
        for (var i = range.Start.Value; i < range.End.Value; i++)
        {
            var newRevisionEvent = events[i] as NewRevisionEvent;
            Debug.Assert(newRevisionEvent is not null);

            try
            {
                newRevisionEvent.Db.Analyze(newRevisionEvent.Prev, _analyzers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to analyze db");
            }

            _dbStream.OnNext(newRevisionEvent.Db);
            ProcessObservers(newRevisionEvent.Db);
            newRevisionEvent.OnFinished.Set();
        }
    }

    private void ObserveAll(List<IEvent> events, Range range)
    {
        const int maxObserversBeforeParallel = 32;
        if (range.GetOffsetAndLength(events.Count).Length <= maxObserversBeforeParallel)
        {
            for (var i = range.Start.Value; i < range.End.Value; i++)
            {
                var observeDatomsEvent = events[i] as IObserveDatomsEvent;
                Debug.Assert(observeDatomsEvent is not null);

                var (from, to, observer) = observeDatomsEvent.Prime(Db);
                _datomObservers.Add(from, to, observer);
            }
        }
        else
        {
            _pendingObservers.Clear();

            Parallel.For(fromInclusive: range.Start.Value, toExclusive: range.End.Value, i =>
            {
                var observeDatomsEvent = events[i] as IObserveDatomsEvent;
                Debug.Assert(observeDatomsEvent is not null);

                var (from, to, observer) = observeDatomsEvent.Prime(Db);
                _pendingObservers.Add((from, to, observer));
            });

            foreach (var (from, to, observer) in _pendingObservers)
            {
                _datomObservers.Add(from, to, observer);
            }

            _pendingObservers.Clear();
        }
    }

    private void UnsubscribeAll(List<IEvent> events, Range range)
    {
        _datomObservers.RemoveWhere(static (observer, state) =>
        {
            for (var i = state.range.Start.Value; i < state.range.End.Value; i++)
            {
                var unSubscribeEvent = state.events[i] as UnSubscribeEvent;
                Debug.Assert(unSubscribeEvent is not null);

                if (ReferenceEquals(unSubscribeEvent.Observer, observer)) return true;
            }

            return false;
        }, (events, range));
    }

    /// <summary>
    /// Scrubs the transaction stream so that we only ever move forward and never repeat transactions
    /// </summary>
    private IDisposable ProcessUpdates(IObservable<IDb> dbStream)
    {
        IDb? prev = null;

        return dbStream.Subscribe(idb =>
        {
            if (_isDisposed) return;

            var db = idb;
            db.Connection = this;

            using var manualResetEventSlim = new ManualResetEventSlim();

            try
            {
                _pendingEvents.Add(new NewRevisionEvent(prev, db, manualResetEventSlim), _cts.Token);
            }
            catch (Exception e) when (e is OperationCanceledException or InvalidOperationException)
            {
                return;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Exception adding event to queue");
                return;
            }

            try
            {
                manualResetEventSlim.Wait(cancellationToken: _cts.Token);
            }
            catch (Exception e) when (e is OperationCanceledException or InvalidOperationException)
            {
                return;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Exception waiting on semaphore");
                return;
            }

            _dbInlet.Values = [db];
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
    public Topology Topology { get; }

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
        var snapshot = new AsOfSnapshot((Snapshot)_store.GetSnapshot(), txId, AttributeCache);
        return snapshot.MakeDb(txId, AttributeCache, this);
    }

    /// <inheritdoc />
    public IDb History()
    {
        return new HistorySnapshot((Snapshot)_store.GetSnapshot(), AttributeCache).MakeDb(TxId, AttributeCache, this);
    }

    /// <inheritdoc />
    public IMainTransaction BeginTransaction()
    {
        return new Transaction(connection: this);
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
    public async Task<ICommitResult> FlushAndCompact(bool verify = false)
    {
        var tx = new Transaction(this);
        tx.Set(new FlushAndCompact(verify));
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
            if (_isDisposed) return Disposable.Empty;

            try
            {
                _pendingEvents.Add(new ObserveDatomsEvent<TDescriptor>(descriptor, observer), _cts.Token);
            }
            catch (Exception e) when (e is OperationCanceledException or InvalidOperationException)
            {
                return Disposable.Empty;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Exception adding event to queue");
                return Disposable.Empty;
            }

            return Disposable.Create((observer, this), static state =>
            {
                var (sObserver, self) = state;
                
                try
                {
                    self._pendingEvents.Add(new UnSubscribeEvent(sObserver), self._cts.Token);
                }
                catch (Exception e) when (e is OperationCanceledException or InvalidOperationException)
                {
                    return;
                }
                catch (Exception e)
                {
                    self._logger.LogError(e, "Exception adding event to queue");
                    return;
                }
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
            initialDb.Connection = this;
            AttributeCache.Reset(initialDb);
            initialDb.Analyze(null, _analyzers);
            
            var declaredAttributes = AttributeResolver.DefinedAttributes.OrderBy(a => a.Id.Id).ToArray();
            _dbStream.OnNext(initialDb);
            
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
    
    private bool _isDisposed;
    private readonly InletNode<IDb> _dbInlet;

    /// <inheritdoc />
    public void Dispose()
    {
        if (_isDisposed) return;

        _cts.Cancel();

        _dbStreamDisposable?.Dispose();
        _pendingEvents.CompleteAdding();
        _pendingEvents.Dispose();

        _isDisposed = true;
    }
}
