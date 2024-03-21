using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reactive.Subjects;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Storage.DatomStorageStructures;
using NexusMods.EventSourcing.Storage.Indexes;
using Reloaded.Memory.Extensions;
using RocksDbSharp;

namespace NexusMods.EventSourcing.Storage;


public class DatomStore : IDatomStore
{
    private readonly AttributeRegistry _registry;
    private readonly ILogger<DatomStore> _logger;
    private readonly Channel<PendingTransaction> _txChannel;
    private EntityId _nextEntityId;
    private readonly Subject<(TxId TxId, IReadDatom[] Datoms)> _updatesSubject;
    private readonly DatomStoreSettings _settings;
    private readonly RocksDb _db;

    #region Indexes
    private readonly TxLog _txLog;

    private readonly EATVCurrent _eatvCurrent;
    private readonly EATVHistory _eatvHistory;
    private readonly AETVCurrent _aetvCurrent;
    private readonly BackrefHistory _backrefHistory;







    #endregion


    private TxId _asOfTxId = TxId.MinValue;
    private readonly PooledMemoryBufferWriter _writer;


    public DatomStore(ILogger<DatomStore> logger, AttributeRegistry registry, DatomStoreSettings settings)
    {
        var options = new DbOptions()
            .SetCreateIfMissing()
            .SetCreateMissingColumnFamilies()
            .SetCompression(Compression.Zstd);

        var columnFamilies = new ColumnFamilies();

        _txLog = new TxLog(registry, columnFamilies);
        _eatvCurrent = new EATVCurrent(registry, columnFamilies);
        _eatvHistory = new EATVHistory(registry, columnFamilies);
        _aetvCurrent = new AETVCurrent(registry, columnFamilies);
        _backrefHistory = new BackrefHistory(registry, columnFamilies);

        _db = RocksDb.Open(options, settings.Path.ToString(), columnFamilies);

        _txLog.Init(_db);
        _eatvCurrent.Init(_db);
        _eatvHistory.Init(_db);
        _aetvCurrent.Init(_db);
        _backrefHistory.Init(_db);

        _writer = new PooledMemoryBufferWriter();


        _logger = logger;
        _settings = settings;
        _registry = registry;
        _nextEntityId = EntityId.From(Ids.MinId(Ids.Partition.Entity) + 1);

        _updatesSubject = new Subject<(TxId TxId, IReadDatom[] Datoms)>();

        registry.Populate(BuiltInAttributes.Initial);

        _txChannel = Channel.CreateUnbounded<PendingTransaction>();
        var _ = Bootstrap();
        Task.Run(ConsumeTransactions);
    }

    private async Task ConsumeTransactions()
    {
        var sw = Stopwatch.StartNew();
        while (await _txChannel.Reader.WaitToReadAsync())
        {
            var pendingTransaction = await _txChannel.Reader.ReadAsync();
            try
            {
                // Sync transactions have no data, and are used to verify that the store is up to date.
                if (pendingTransaction.Data.Length == 0)
                {
                    pendingTransaction.AssignedTxId = _asOfTxId;
                    pendingTransaction.CompletionSource.SetResult(_asOfTxId);
                    continue;
                }

                Log(pendingTransaction, out var readAbles);

                pendingTransaction.CompletionSource.SetResult(_asOfTxId);

                //_logger.LogDebug("Transaction {TxId} processed in {Elapsed}ms, new in-memory size is {Count} datoms", pendingTransaction.AssignedTxId!.Value, sw.ElapsedMilliseconds, _indexes.InMemorySize);
            }
            catch (Exception ex)
            {
                pendingTransaction.CompletionSource.TrySetException(ex);
            }
        }
    }

    /// <summary>
    /// Sets up the initial state of the store.
    /// </summary>
    private async Task Bootstrap()
    {
        try
        {
            var lastTx = GetMostRecentTxId();
            if (lastTx == TxId.MinValue)
            {
                _logger.LogInformation("Bootstrapping the datom store no existing state found");
                var _ = await Transact(BuiltInAttributes.InitialDatoms);
                return;
            }
            else
            {
                _logger.LogInformation("Bootstrapping the datom store, existing state found, last tx: {LastTx}", lastTx.Value.ToString("x"));
                _asOfTxId = lastTx;
            }

            _nextEntityId = EntityId.From(GetMaxEntityId().Value + 1);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to bootstrap the datom store");
        }
    }

    /*
    private EntityId GetLastEntityId(DatomStoreState indexes)
    {
        var toFind = new Datom()
        {
            E = EntityId.From(Ids.MakeId(Ids.Partition.Entity, ulong.MaxValue)),
            A = AttributeId.From(ulong.MinValue),
            T = TxId.MaxValue,
            F = DatomFlags.Added,
            V = Array.Empty<byte>()
        };

        ulong startValue = 0;

        var startInMemory = indexes.EAVT.InMemory.FindEATV(0, indexes.EAVT.InMemory.Length, toFind, _registry);
        if (startInMemory == indexes.EAVT.InMemory.Length)
        {
            startValue = 0;
        }
        else
        {
            startValue = indexes.EAVT.InMemory[startInMemory].E.Value;
        }

        var startHistory = indexes.EAVT.History.FindEATV(0, indexes.EAVT.History.Length, toFind, _registry);
        if (startHistory == indexes.EAVT.History.Length)
        {
            startValue = 0;
        }
        else
        {
            var historyValue = indexes.EAVT.History[startHistory].E.Value;
            if (historyValue > startValue)
            {
                startValue = historyValue;
            }
        }

        if (startValue == 0)
            return EntityId.From(Ids.MakeId(Ids.Partition.Entity, 0));


        var entityInMemory = indexes.EAVT.InMemory[startInMemory].E;
        var entityHistory = indexes.EAVT.History[startHistory].E;

        var max =  EntityId.From(Math.Max(entityHistory.Value, entityInMemory.Value));

        if (!Ids.IsPartition(max.Value, Ids.Partition.Entity))
        {
            throw new InvalidOperationException("Invalid max id");
        }

        return max;
    }
    */

    public TxId AsOfTxId => _asOfTxId;

    public void Dispose()
    {
        _txChannel.Writer.Complete();
        _db.Dispose();
        _txLog.Dispose();
        _eatvCurrent.Dispose();
        _eatvHistory.Dispose();
        _aetvCurrent.Dispose();
        _backrefHistory.Dispose();
    }

    public async Task<TxId> Sync()
    {
        await Transact(Enumerable.Empty<IWriteDatom>());
        return _asOfTxId;
    }

    public async Task<DatomStoreTransactResult> Transact(IEnumerable<IWriteDatom> datoms)
    {
        var pending = new PendingTransaction { Data = datoms.ToArray() };
        if (!_txChannel.Writer.TryWrite(pending))
            throw new InvalidOperationException("Failed to write to the transaction channel");

        await pending.CompletionSource.Task;

        return new DatomStoreTransactResult(pending.AssignedTxId!.Value, pending.Remaps);
    }

    public IObservable<(TxId TxId, IReadDatom[] Datoms)> TxLog => _updatesSubject;


    public IEnumerable<Datom> Where<TAttr>(TxId txId) where TAttr : IAttribute
    {
        throw new NotImplementedException();
    }


    public IEnumerable<Datom> Where(TxId txId, EntityId id)
    {
        throw new NotImplementedException();
    }

    public IEnumerable<IReadDatom> Resolved(IEnumerable<Datom> datoms)
    {
        return datoms.Select(datom => _registry.Resolve(datom));
    }

    public async Task RegisterAttributes(IEnumerable<DbAttribute> newAttrs)
    {
        var datoms = new List<IWriteDatom>();
        var newAttrsArray = newAttrs.ToArray();

        foreach (var attr in newAttrsArray)
        {
            datoms.Add(BuiltInAttributes.UniqueId.Assert(EntityId.From(attr.AttrEntityId.Value), attr.UniqueId));
            datoms.Add(BuiltInAttributes.ValueSerializerId.Assert(EntityId.From(attr.AttrEntityId.Value), attr.ValueTypeId));
        }

        await Transact(datoms);

        _registry.Populate(newAttrsArray);
    }

    public Expression GetValueReadExpression(Type attribute, Expression valueSpan, out AttributeId attributeId)
    {
        return _registry.GetReadExpression(attribute, valueSpan, out attributeId);
    }

    public IEnumerable<EntityId> GetReferencesToEntityThroughAttribute<TAttribute>(EntityId id, TxId txId)
        where TAttribute : IAttribute<EntityId>
    {
           return _backrefHistory.GetReferencesToEntityThroughAttribute<TAttribute>(id, txId);
    }



    public bool TryGetExact<TAttr, TValue>(EntityId e, TxId tx, out TValue val) where TAttr : IAttribute<TValue>
    {
        if (_eatvHistory.TryGetExact<TAttr, TValue>(e, tx, out var foundVal))
        {
            val = foundVal;
            return true;
        }
        val = default!;
        return false;
    }

    public bool TryGetLatest<TAttribute, TValue>(EntityId e, TxId tx, out TValue value)
        where TAttribute : IAttribute<TValue>
    {
        if (_eatvCurrent.TryGet<TAttribute, TValue>(e, tx, out var foundVal) == LookupResult.Found)
        {
            value = foundVal;
            return true;
        }

        if (_eatvHistory.TryGetLatest<TAttribute, TValue>(e, tx, out foundVal))
        {
            value = foundVal;
            return true;
        }

        value = default!;
        return false;
    }

    public IEnumerable<EntityId> GetEntitiesWithAttribute<TAttribute>(TxId txId)
        where TAttribute : IAttribute
    {
        return _aetvCurrent.GetEntitiesWithAttribute<TAttribute>(txId);
    }

    public IEnumerable<IReadDatom> GetAttributesForEntity(EntityId realId, TxId txId)
    {
        foreach (var datom in _eatvCurrent.GetAttributesForEntity(realId, txId))
        {
            yield return datom;
        }
    }

    /// <summary>
    /// Gets the maximum entity id in the store.
    /// </summary>
    public EntityId GetMaxEntityId()
    {
        return _eatvCurrent.GetMaxEntityId();
    }

    /// <summary>
    /// Gets the most recent transaction id.
    /// </summary>
    public TxId GetMostRecentTxId()
    {
        return _txLog.GetMostRecentTxId();
    }

    public Type GetReadDatomType(Type attribute)
    {
        return _registry.GetReadDatomType(attribute);
    }


    #region Internals


    EntityId MaybeRemap(EntityId id, PendingTransaction pendingTransaction, TxId thisTx)
    {
        if (Ids.GetPartition(id) == Ids.Partition.Tmp)
        {
            if (!pendingTransaction.Remaps.TryGetValue(id, out var newId))
            {
                if (id.Value == Ids.MinId(Ids.Partition.Tmp))
                {
                    var remapTo = EntityId.From(thisTx.Value);
                    pendingTransaction.Remaps.Add(id, remapTo);
                    return remapTo;
                }
                else
                {
                    pendingTransaction.Remaps.Add(id, _nextEntityId);
                    var remapTo = _nextEntityId;
                    _nextEntityId = EntityId.From(_nextEntityId.Value + 1);
                    return remapTo;
                }
            }
            else
            {
                return newId;
            }
        }
        return id;
    }



    private void Log(PendingTransaction pendingTransaction, out IWriteDatom[] node)
    {
        var thisTx = TxId.From(_asOfTxId.Value + 1);


        var stackDatom = new StackDatom();

        var remapFn = (Func<EntityId, EntityId>)(id => MaybeRemap(id, pendingTransaction, thisTx));
        using var batch = new WriteBatch();

        var swPrepare = Stopwatch.StartNew();
        foreach (var datom in pendingTransaction.Data)
        {
            _writer.Reset();
            _writer.Advance(StackDatom.PaddingSize);
            datom.Explode(_registry, remapFn, ref stackDatom, _writer);
            stackDatom.T = thisTx.Value;
            stackDatom.PaddedSpan = _writer.GetWrittenSpanWritable();
            stackDatom.V = stackDatom.PaddedSpan.SliceFast(StackDatom.PaddingSize);

            _txLog.Add(batch, ref stackDatom);
            _eatvHistory.Add(batch, ref stackDatom);
            _eatvCurrent.Add(batch, ref stackDatom);
            _aetvCurrent.Add(batch, ref stackDatom);

            if (_registry.IsReference(AttributeId.From(stackDatom.A)))
                _backrefHistory.Add(batch, ref stackDatom);
        }

        var swWrite = Stopwatch.StartNew();
        _db.Write(batch);

        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug("Transaction {TxId} ({Count} datoms) prepared in {Elapsed}ms, written in {WriteElapsed}ms",
                thisTx.Value,
                pendingTransaction.Data.Length,
                swPrepare.ElapsedMilliseconds - swWrite.ElapsedMilliseconds,
                swWrite.ElapsedMilliseconds);
        }


        _asOfTxId = thisTx;
        pendingTransaction.AssignedTxId = thisTx;
        node = [];

    }

    #endregion
}
