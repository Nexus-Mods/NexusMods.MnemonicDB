using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.BuiltInEntities;
using NexusMods.MnemonicDB.Abstractions.DatomIterators;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.IndexSegments;
using NexusMods.MnemonicDB.Abstractions.Query;
using NexusMods.MnemonicDB.Abstractions.TxFunctions;
using ObservableExtensions = R3.ObservableExtensions;

namespace NexusMods.MnemonicDB;

/// <summary>
///     Main connection class, co-ordinates writes and immutable reads
/// </summary>
public class Connection : IConnection
{
    private readonly IDatomStore _store;
    private readonly ILogger<Connection> _logger;

    private DbStream _dbStream;
    private IDisposable? _dbStreamDisposable;
    private readonly IAnalyzer[] _analyzers;

    /// <summary>
    ///     Main connection class, co-ordinates writes and immutable reads
    /// </summary>
    public Connection(ILogger<Connection> logger, IDatomStore store, IServiceProvider provider, IEnumerable<IAnalyzer> analyzers)
    {
        ServiceProvider = provider;
        AttributeCache = store.AttributeCache;
        AttributeResolver = new AttributeResolver(provider, AttributeCache);
        _logger = logger;
        _store = store;
        _dbStream = new DbStream();
        _analyzers = analyzers.ToArray();
        Bootstrap();
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
            prev = idb;

            return idb;
        });
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
        return new Db(new HistorySnapshot(_store.GetSnapshot(), TxId, AttributeCache), TxId, AttributeCache)
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
    public async Task<ulong> Excise(EntityId[] entityIds)
    {
        // Retract all datoms for the given entity ids
        var contextDb = Db;
        
        List<Datom> datomsToRemove = new();
        foreach (var entityId in entityIds)
        {
            var segment = contextDb.Datoms(entityId);
            datomsToRemove.AddRange(segment);
        }

        {
            using var tx = BeginTransaction();
            foreach (var datom in datomsToRemove)
            {
                tx.Add(datom.Retract());
            }
            var results = await tx.Commit();
            contextDb = results.Db;
        }
        
        // Now delete all the datoms from all indexes
        datomsToRemove.Clear();
        
        foreach (var entityId in entityIds)
        {
            var segment = contextDb.Datoms(SliceDescriptor.Create(IndexType.EAVTHistory, entityId));
            datomsToRemove.AddRange(segment);
        }
        
        await _store.Excise(datomsToRemove);

        {
            using var tx = BeginTransaction();
            tx.Add((EntityId)tx.ThisTxId.Value, Abstractions.BuiltInEntities.Transaction.ExcisedDatoms, (ulong)datomsToRemove.Count);
            await tx.Commit();
        }
        
        
        return (ulong)datomsToRemove.Count;
    }

    /// <inheritdoc />
    public IObservable<IDb> Revisions => _dbStream;

    private void AddMissingAttributes()
    {
        var declaredAttributes = AttributeResolver.DefinedAttributes;
        var existing = AttributeCache.AllAttributeIds.ToHashSet();
        
        if (existing.Count == 0)
            throw new AggregateException(
                "No attributes found in the database, something went wrong, as it should have been bootstrapped by now");

        var missing = declaredAttributes.Where(a => !existing.Contains(a.Id)).ToArray();
        if (missing.Length == 0)
        {
            // No changes to make to the schema, we can return early
            return;
        }
        
        var attrId = existing.Select(sym => AttributeCache.GetAttributeId(sym)).Max().Value;
        using var builder = new IndexSegmentBuilder(AttributeCache);
        foreach (var attr in missing.OrderBy(e => e.Id.Id))
        {
            var id = EntityId.From(++attrId);
            builder.Add(id, AttributeDefinition.UniqueId, attr.Id);
            builder.Add(id, AttributeDefinition.ValueType, attr.LowLevelType);
            if (attr.IsIndexed)
                builder.Add(id, AttributeDefinition.Indexed, Null.Instance);
            builder.Add(id, AttributeDefinition.Cardinality, attr.Cardinalty);
            if (attr.NoHistory)
                builder.Add(id, AttributeDefinition.NoHistory, Null.Instance);
            if (attr.DeclaredOptional)
                builder.Add(id, AttributeDefinition.Optional, Null.Instance);
        }

        var (_, db) = _store.Transact(builder.Build());
        AttributeCache.Reset(db);
    }

    internal async Task<ICommitResult> Transact(IndexSegment datoms, HashSet<ITxFunction>? txFunctions)
    {
        StoreResult newTx;
        IDb newDb;

        (newTx, newDb) = await _store.TransactAsync(datoms, txFunctions);
        ((Db)newDb).Connection = this;
        var result = new CommitResult(newDb, newTx.Remaps);
        return result;
    }

    private void Bootstrap()
    {
        try
        {
            var initialSnapshot = _store.GetSnapshot();
            var initialDb = new Db(initialSnapshot, TxId, AttributeCache)
            {
                Connection = this
            };
            AttributeCache.Reset(initialDb);
            
            AddMissingAttributes();

            _dbStreamDisposable = ProcessUpdates(_store.TxLog)
                .Subscribe(itm => _dbStream.OnNext(itm));
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
