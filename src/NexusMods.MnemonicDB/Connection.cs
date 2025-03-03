﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using DynamicData;
using Jamarino.IntervalTree;
using Microsoft.Extensions.Logging;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.DatomIterators;
using NexusMods.MnemonicDB.Abstractions.Query;
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

    private DbStream _dbStream;
    private IDisposable? _dbStreamDisposable;
    private readonly IAnalyzer[] _analyzers;
    
    // Temporary storage for processing observers, we store these in the class so we don't have to 
    // allocate them on each update, and the code that uses these is always run on the same thread.
    private readonly LightIntervalTree<Datom, IObserver<DatomChangeSet>> _datomObservers = new();
    
    private readonly Dictionary<IObserver<DatomChangeSet>, DatomChangeSet> _changeSets = new();
    private readonly List<Change<Datom, DatomKey>> _localChanges = [];
    private readonly ConcurrentQueue<IObserver<DatomChangeSet>> _pendingDisposalObservables = new();
    
    private readonly BlockingCollection<Action> _pendingEvents = new(new ConcurrentQueue<Action>());
    private Thread _eventThread = default!;

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
        while (!_pendingEvents.IsCompleted)
        {
            if (!_pendingEvents.TryTake(out var action, 1000))
                continue;
            
            try
            {
                action();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process event");
            }
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
            var db = (Db)idb;
            db.Connection = this;
            var tcs = new TaskCompletionSource();
            _pendingEvents.Add(() =>
            {
                foreach (var analyzer in _analyzers)
                {
                    try
                    {
                        var result = analyzer.Analyze(prev, idb);
                        db.AnalyzerData.Add(analyzer.GetType(), result);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to analyze with {Analyzer}", analyzer.GetType().Name);
                    }
                }

                _dbStream.OnNext(db);
                ProcessObservers(db);
                prev = idb;
                tcs.SetResult();
            });
            tcs.Task.Wait();
        });
    }

    private void ProcessObservers(Db db)
    {
        ProcessDisposedObservers();
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
        return new Db(snapshot, txId, AttributeCache)
        {
            Connection = this
        };
    }

    /// <inheritdoc />
    public IDb History()
    {
        return new Db(new HistorySnapshot(_store.GetSnapshot(), AttributeCache), TxId, AttributeCache)
        {
            Connection = this
        };
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
            _pendingEvents.Add(() => {
                var (fromDatom, toDatom, isReversed) = descriptor;
                
                while (true)
                {
                    var db = Db;
                    var datoms = db.Datoms(descriptor);
                    var cache = db.AttributeCache;
                    var changes = new DatomChangeSet(db);

                    foreach (var datom in datoms)
                    {
                        var isMany = cache.IsCardinalityMany(datom.A);
                        changes.Add(new Change<Datom, DatomKey>(ChangeReason.Add, CreateKey(datom, datom.A, isMany),
                            datom));
                    }

                    // In the rare case where we weren't able to prime the first change set fast enough, we'll need to re-run
                    // the changeset generation
                    if (Db.BasisTxId != db.BasisTxId)
                        continue;
                    observer.OnNext(changes);
                    break;
                }

                // Add the observer, from here we could possibly start getting updates, we'll assume that between the above
                // "onNext" and here, another transaction won't go through. 
                _datomObservers.Add(fromDatom, toDatom, observer);
            
            });
            
            return Disposable.Create((observer, this), static state =>
            {
                // Enqueue the dispose operation, we don't remove the item here, because removing a lot of items one by one
                // from the tree is expensive, so we batch them up and remove them all at once.
                var (s, self) = state;
                self._pendingDisposalObservables.Enqueue(s);
            });
        });
    }
    
    /// <summary>
    /// Removing items from the interval tree is a O(n) operation, so we batch them up and remove them all at once to keep
    /// this from becoming a O(n * m) operation.
    /// </summary>
    private void ProcessDisposedObservers()
    {
        // Quick exit so we don't allocate an enumerator
        if (_pendingDisposalObservables.IsEmpty)
            return;

        var toDispose = new HashSet<IObserver<DatomChangeSet>>();

        // Dequeue all the observers that need to be disposed
        foreach (var itm in _pendingDisposalObservables)
            toDispose.Add(itm);

        _datomObservers.RemoveWhere(static (observer, set) => set.Contains(observer), toDispose);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static DatomKey CreateKey(Datom datom, AttributeId attrId, bool isMany = false)
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
        ((Db)newDb).Connection = this;
        var result = new CommitResult(newDb, newTx.Remaps);
        return result;
    }

    private void Bootstrap(bool readOnlyMode)
    {
        try
        {
            var initialSnapshot = _store.GetSnapshot();
            var initialDb = new Db(initialSnapshot, TxId, AttributeCache)
            {
                Connection = this
            };
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
        _pendingEvents.CompleteAdding();
        _eventThread.Join();
    }
}
