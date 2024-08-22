using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.BuiltInEntities;
using NexusMods.MnemonicDB.Abstractions.IndexSegments;
using NexusMods.MnemonicDB.Abstractions.Internals;
using NexusMods.MnemonicDB.Abstractions.Query;
using NexusMods.MnemonicDB.Abstractions.TxFunctions;
using NexusMods.MnemonicDB.Storage;

namespace NexusMods.MnemonicDB;

/// <summary>
///     Main connection class, co-ordinates writes and immutable reads
/// </summary>
public class Connection : IConnection
{
    private readonly IDatomStore _store;
    private readonly Dictionary<Symbol, IAttribute> _declaredAttributes;
    private readonly ILogger<Connection> _logger;

    private BehaviorSubject<IDb> _dbStream;
    private IDisposable? _dbStreamDisposable;
    private readonly IAnalyzer[] _analyzers;

    /// <summary>
    ///     Main connection class, co-ordinates writes and immutable reads
    /// </summary>
    public Connection(ILogger<Connection> logger, IDatomStore store, IServiceProvider provider, IEnumerable<IAttribute> declaredAttributes, IEnumerable<IAnalyzer> analyzers)
    {
        ServiceProvider = provider;
        _logger = logger;
        _declaredAttributes = declaredAttributes.ToDictionary(a => a.Id);
        _store = store;
        _dbStream = new BehaviorSubject<IDb>(default!);
        _analyzers = analyzers.ToArray();
        Bootstrap();
    }

    /// <summary>
    /// Scrubs the transaction stream so that we only ever move forward and never repeat transactions
    /// </summary>
    private IObservable<Db> ProcessUpdate(IObservable<IDb> dbStream)
    {
        IDb? prev = null;

        return Observable.Create((IObserver<Db> observer) =>
        {
            return dbStream.Subscribe(nextItem =>
            {
                
                if (prev != null && prev.BasisTxId >= nextItem.BasisTxId)
                    return;

                var db = (Db)nextItem;
                db.Connection = this;
                
                foreach (var analyzer in _analyzers)
                {
                    try
                    {
                        var result = analyzer.Analyze(prev, nextItem);
                        db.AnalyzerData.Add(analyzer.GetType(), result);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to analyze with {Analyzer}", analyzer.GetType().Name);
                    }
                }
                
                observer.OnNext((Db)nextItem);
                prev = nextItem;
            }, observer.OnError, observer.OnCompleted);
        });
    }

    /// <inheritdoc />
    public IServiceProvider ServiceProvider { get; set; }

    /// <inheritdoc />
    public IDb Db
    {
        get
        {
            var val = _dbStream;
            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            if (val == null)
                ThrowNullDb();
            return val!.Value;
        }
    }

    /// <inheritdoc />
    public IAttributeRegistry Registry => _store.Registry;

    private static void ThrowNullDb()
    {
        throw new InvalidOperationException("Connection not started, did you forget to start the hosted service?");
    }


    /// <inheritdoc />
    public TxId TxId => _store.AsOfTxId;

    /// <inheritdoc />
    public IDb AsOf(TxId txId)
    {
        var snapshot = new AsOfSnapshot(_store.GetSnapshot(), txId, (AttributeRegistry)_store.Registry);
        return new Db(snapshot, txId, (AttributeRegistry)_store.Registry)
        {
            Connection = this
        };
    }

    /// <inheritdoc />
    public ITransaction BeginTransaction()
    {
        return new Transaction(this, _store.Registry);
    }

    /// <inheritdoc />
    public IAnalyzer[] Analyzers => _analyzers;

    /// <inheritdoc />
    public IObservable<IDb> Revisions
    {
        get
        {
            if (_dbStream == default!)
                ThrowNullDb();
            return _dbStream!;
        }
    }

    private void AddMissingAttributes(IEnumerable<IAttribute> declaredAttributes)
    {
        var existing = ExistingAttributes().ToDictionary(a => a.UniqueId);
        if (existing.Count == 0)
            throw new AggregateException(
                "No attributes found in the database, something went wrong, as it should have been bootstrapped by now");

        var missing = declaredAttributes.Where(a => !existing.ContainsKey(a.Id)).ToArray();
        if (missing.Length == 0)
        {
            // Nothing new to assert, so just add the new data to the registry
            _store.Registry.Populate(existing.Values.ToArray());
        }

        var newAttrs = new List<DbAttribute>();

        var attrId = existing.Values.Max(a => a.AttrEntityId).Value;
        foreach (var attr in missing)
        {
            var id = ++attrId;

            var uniqueId = attr.Id;
            newAttrs.Add(new DbAttribute(uniqueId, AttributeId.From(id), attr.LowLevelType, attr));
        }

        _store.RegisterAttributes(newAttrs);
    }

    private IEnumerable<DbAttribute> ExistingAttributes()
    {
        var db = new Db(_store.GetSnapshot(), TxId, (AttributeRegistry)_store.Registry)
        {
            Connection = this
        };

        foreach (var attribute in AttributeDefinition.All(db))
        {
            var declared = _declaredAttributes[attribute.UniqueId];
            yield return new DbAttribute(attribute.UniqueId, AttributeId.From((ushort)attribute.Id.Value), attribute.ValueType, declared);
        }
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
            AddMissingAttributes(_declaredAttributes.Values);

            _dbStreamDisposable = ProcessUpdate(_store.TxLog)
                .Subscribe(_dbStream);
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
