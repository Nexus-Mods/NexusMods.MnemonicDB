using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using DynamicData;
using Jamarino.IntervalTree;
using Microsoft.Extensions.Logging;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.DatomIterators;
using NexusMods.MnemonicDB.Abstractions.IndexSegments;
using NexusMods.MnemonicDB.Abstractions.Query;
using NexusMods.MnemonicDB.InternalTxFunctions;
using NexusMods.MnemonicDB.Storage;
using Disposable = R3.Disposable;
using Observable = System.Reactive.Linq.Observable;

namespace NexusMods.MnemonicDB;

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
    
    private List<Change<Datom, DatomKey>> _changes = new();
    
    // Temporary storage for processing observers, we store these in the class so we don't have to 
    // allocate them on each update, and the code that uses these is always run on the same thread.
    private readonly LightIntervalTree<Datom, IObserver<(IChangeSet<Datom, DatomKey> Changes, IDb Db)>> _datomObservers = new();
    private readonly Dictionary<IObserver<(IChangeSet<Datom, DatomKey> Changes, IDb Db)>, ChangeSet<Datom, DatomKey>> _changeSets = new();
    private readonly ConcurrentQueue<IObserver<(IChangeSet<Datom, DatomKey> Changes, IDb Db)>> _observersPendingDisposal = [];
    
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

    /// <summary>
    /// Scrubs the transaction stream so that we only ever move forward and never repeat transactions
    /// </summary>
    private IObservable<IDb> ProcessUpdates(IObservable<IDb> dbStream)
    {
        IDb? prev = null;

        return dbStream.Select(idb =>
        {
            var db = (Db)idb;
            db.Connection = this;
                
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
            

            return idb;
        });
    }

    private void ProcessObservers(Db db)
    {
        var recentlyAdded = db.RecentlyAdded;
        var cache = db.AttributeCache;
        _changeSets.Clear();
        _changes.Clear();
            
        DatomsToChanges(recentlyAdded, cache, _changes);

        // We now have collected all the changes, so we need to group them into changesets and send them to the listeners.
        // We start by locking the observers, so we can safely process them. 
        _changeSets.Clear();
        lock (_datomObservers)
        {
            ProcessDisposedObservers();
            foreach (var change in _changes)
            {
                // It sucks that we have to do this for each index type, but we don't have a way to do a fuzzy match on the
                // index part of the datom key, so we have to do it this way.
                foreach (var index in IndexTypes)
                {
                    var reindex = change.Current.WithIndex(index);
                    var matches = _datomObservers.Query(reindex);
                    foreach (var overlap in matches)
                    {
                        ref var changeSet = ref CollectionsMarshal.GetValueRefOrAddDefault(_changeSets, overlap, out _);
                        changeSet ??= [];
                        changeSet.Add(change);
                    }
                }
            }
        }
            
        // Now that the changesets are built, we can send them to the observers. There is a race condition here, because
        // between the time that we built the changesets and the time we send them, an observer could have been registered. 
        // But we want to release sends outside the lock, so that any subscribers that try to connect inside a send don't
        // deadlock.
        foreach (var (subject, changeSet) in _changeSets)
        {
            subject.OnNext((changeSet, Db));
        }
    }

    /// <summary>
    /// Given an index segment of recently added datoms, we need to convert them into changes that observers may be interested in.
    /// </summary>
    private static void DatomsToChanges(IndexSegment recentlyAdded, AttributeCache cache, List<Change<Datom, DatomKey>> changes)
    {
        // For each recently added datom, we need to find listeners to it
        for (var i = 0; i < recentlyAdded.Count; i++)
        {
            var datom = recentlyAdded[i];
                
            var isMany = cache.IsCardinalityMany(datom.A);
            var attr = datom.A;
            if (datom.IsRetract)
            {
                    
                // If the attribute is cardinality many, we can just remove the datom
                if (isMany)
                {
                    changes.Add(new Change<Datom, DatomKey>(ChangeReason.Remove, CreateKey(datom, attr, true), datom));
                    continue;
                }
                    
                // If at the end of the segment
                if (i + 1 >= recentlyAdded.Count)
                {
                    changes.Add(new Change<Datom, DatomKey>(ChangeReason.Remove, CreateKey(datom, attr), datom));
                    continue;
                }

                // If the next datom is not the same E or A, we can remove the datom
                var nextDatom = recentlyAdded[i + 1];
                if (nextDatom.E != datom.E || nextDatom.A != datom.A)
                {
                    changes.Add(new Change<Datom, DatomKey>(ChangeReason.Remove, CreateKey(datom, attr), datom));
                    continue;
                }

                // Otherwise we skip the add, and issue an update, and skip the add because we've already processed it
                changes.Add(new Change<Datom, DatomKey>(ChangeReason.Update, CreateKey(datom, attr), nextDatom, datom));
                i++;
                continue;
            }
            else
            {
                changes.Add(new Change<Datom, DatomKey>(ChangeReason.Add, CreateKey(datom, attr, isMany), datom));
            }
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

    public IObservable<IChangeSet<Datom, DatomKey>> ObserveDatoms(SliceDescriptor descriptor)
    {
        return Observable.Create<IChangeSet<Datom, DatomKey>>(observer =>
        {
            var lastDb = new Box<TxId>(TxId.MinValue);
            var subject = new Subject<(IChangeSet<Datom, DatomKey> Changes, IDb Db)>();
            var fromDatom = descriptor.From.WithIndex(descriptor.Index);
            var toDatom = descriptor.To.WithIndex(descriptor.Index);
            lock (_datomObservers)
            {
                ProcessDisposedObservers();
                _datomObservers.Add(fromDatom, toDatom, subject);
            }

            var disposable = subject.Subscribe(changeSet =>
            {
                lock (lastDb)
                {
                    IChangeSet<Datom, DatomKey> changes;
                    
                    // First time we've seen a change, so send all the datoms
                    if (lastDb.Value == TxId.MinValue)
                    {
                        changes = InitializeFrom(changeSet.Db, fromDatom, toDatom);
                    }
                    // Normal case, where the changeset we're given is valid
                    else if (changeSet.Db.BasisTxId.Value == lastDb.Value.Value + 1)
                        changes = changeSet.Changes;
                    // Somehow we got an older changeset, so we'll drop it
                    else if (changeSet.Db.BasisTxId <= lastDb.Value)
                        return;
                    // We somehow fell behind by one or more timestamps, so re-diff all the ones we missed, then send the most recent
                    // changeset
                    else
                    {
                        for (var txId = lastDb.Value.Value + 1; txId < changeSet.Db.BasisTxId.Value - 1; txId++)
                        {
                            var db = AsOf(TxId.From(txId));
                            observer.OnNext(ProcessDiff(db, fromDatom, toDatom));
                        }
                        changes = changeSet.Changes;
                    }

                    lastDb.Value = changeSet.Db.BasisTxId;
                    
                    if (changes.Count > 0)
                    {
                        observer.OnNext(changes);
                    }
                }
            });

            // Prime the subject, this could possibly race with the subscription, but that's fine because we have the 
            // ability to remove duplicates, and 
            subject.OnNext((ChangeSet<Datom, DatomKey>.Empty, Db));

            return Disposable.Create((disposable, subject, this), static state =>
            {
                state.disposable.Dispose();
                state.Item3._observersPendingDisposal.Enqueue(state.subject);
            });
        });
        
        
        IChangeSet<Datom, DatomKey> ProcessDiff(IDb db, Datom fromDatom, Datom toDatom)
        {
            var descriptor = SliceDescriptor.Create(fromDatom, toDatom);
            var changes = new List<Change<Datom, DatomKey>>();
            DatomsToChanges(db.RecentlyAdded, db.AttributeCache, changes);
            var changeSet = new ChangeSet<Datom, DatomKey>();
            foreach (var change in changes)
            {
                if (descriptor.Includes(change.Current))
                {
                    changeSet.Add(change);
                }
            }
            return changeSet;
        }            
        
        // We need to send all datoms in the db that match the given range
        static IChangeSet<Datom, DatomKey> InitializeFrom(IDb db, Datom fromDatom, Datom toDatom)
        {
            var changeSet = new ChangeSet<Datom, DatomKey>();
            var cache = db.AttributeCache;
            
            foreach (var datom in db.Datoms(SliceDescriptor.Create(fromDatom, toDatom)))
            {
                var isCardinalityMany = cache.IsCardinalityMany(datom.A);
                changeSet.Add(new Change<Datom, DatomKey>(ChangeReason.Add, CreateKey(datom, datom.A, isCardinalityMany), datom));
            }
            return changeSet;
        }
        
    }

    private void ProcessDisposedObservers()
    {
        // Quick exit so we don't allocate an enumerator
        if (_observersPendingDisposal.IsEmpty)
            return;
        
        var toDispose = new HashSet<IObserver<(IChangeSet<Datom, DatomKey> Changes, IDb Db)>>();
        
        // Dequeue all the observers that need to be disposed
        foreach (var itm in _observersPendingDisposal)
            toDispose.Add(itm);
        
        // Dispose all the observers
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
            
            if (!readOnlyMode)
                _store.Transact(new SimpleMigration(declaredAttributes));
            
            _dbStreamDisposable = ProcessUpdates(_store.TxLog)
                .Subscribe();
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
}
