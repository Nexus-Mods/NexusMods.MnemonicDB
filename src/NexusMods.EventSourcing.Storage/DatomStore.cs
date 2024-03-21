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
    private EntityId _nextEntId;
    private readonly Subject<(TxId TxId, IReadDatom[] Datoms)> _updatesSubject;
    private readonly DatomStoreSettings _settings;
    private readonly RocksDb _db;

    #region Indexes
    private readonly EATVCurrent _eatvCurrent;
    private readonly EATVHistory _eatvHistory;
    private readonly AETVCurrent _aetvCurrent;




    #endregion


    private TxId _asOfTxId = TxId.MinValue;
    private readonly PooledMemoryBufferWriter _writer;


    public DatomStore(ILogger<DatomStore> logger, AttributeRegistry registry, DatomStoreSettings settings)
    {
        var options = new DbOptions()
            .SetCreateIfMissing()
            .SetCreateMissingColumnFamilies();

        _eatvCurrent = new EATVCurrent(registry);
        _eatvHistory = new EATVHistory(registry);
        _aetvCurrent = new AETVCurrent(registry);

        _db = RocksDb.Open(options, settings.Path.ToString(), new ColumnFamilies());

        _eatvCurrent.Init(_db);
        _eatvHistory.Init(_db);
        _aetvCurrent.Init(_db);

        _writer = new PooledMemoryBufferWriter();


        _logger = logger;
        _settings = settings;
        _registry = registry;
        _nextEntId = EntityId.From(Ids.MinId(Ids.Partition.Entity) + 1);

        _updatesSubject = new Subject<(TxId TxId, IReadDatom[] Datoms)>();

        registry.Populate(BuiltInAttributes.Initial);

        _txChannel = Channel.CreateUnbounded<PendingTransaction>();
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

        /*
        try
        {
            if (!_nodeStore.TryGetLastTx(out var tx))
            {
                _logger.LogInformation("Bootstrapping the datom store no existing state found");
                var _ = await Transact(BuiltInAttributes.InitialDatoms);
                return;
            }

            _logger.LogInformation("Bootstrapping the datom store from tx {TxId}", tx.Value);

            if (_nodeStore.LoadRoot(out var root))
            {
                _indexes = root;
            }


            var txToReplay = tx.Value - _indexes.LastFlushedTxId.Value;
            if (txToReplay > 0)
            {
                _logger.LogInformation("Replaying {TxCount} transactions", txToReplay);
                var sw = Stopwatch.StartNew();
                var replayed = 0;
                for (var thisTx = _indexes.LastFlushedTxId.Value + 1; thisTx <= tx.Value; thisTx++)
                {
                    var key = StoreKey.From(Ids.MakeId(Ids.Partition.TxLog, thisTx));
                    var packed = _nodeStore.Load(key);
                    var appendableNode = AppendableNode.Initialize(packed);
                    await UpdateInMemoryIndexes(appendableNode, TxId.From(thisTx));
                    replayed++;
                }
                _logger.LogInformation("Replayed {TxCount} transactions in {Elapsed}ms new in-memory size is {Datoms} datoms", replayed, sw.ElapsedMilliseconds, _indexes.InMemorySize);
            }

            _nextEntId = GetLastEntityId(_indexes);


        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to bootstrap the datom store");
        }
        */
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

    public TxId AsOfTxId => throw new NotImplementedException();

    public void Dispose()
    {
        _txChannel.Writer.Complete();
        _db.Dispose();
        _eatvCurrent.Dispose();
        _eatvHistory.Dispose();
        _aetvCurrent.Dispose();
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

    public IEnumerable<EntityId> ReverseLookup<TAttribute>(TxId txId, EntityId id) where TAttribute : IAttribute<EntityId>
    {
        throw new NotImplementedException();
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

    public IEnumerable<EntityId> GetEntitiesWithAttribute<TAttribute>()
        where TAttribute : IAttribute
    {
        return _aetvCurrent.GetEntitiesWithAttribute<TAttribute>();
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


    #region Internals


    private void Log(PendingTransaction pendingTransaction, out IWriteDatom[] node)
    {
        var thisTx = TxId.From(_asOfTxId.Value + 1);

        EntityId MaybeRemap(EntityId id)
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
                        pendingTransaction.Remaps.Add(id, _nextEntId);
                        var remapTo = _nextEntId;
                        _nextEntId = EntityId.From(_nextEntId.Value + 1);
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

        var stackDatom = new StackDatom();

        var remapFn = (Func<EntityId, EntityId>)MaybeRemap;
        var batch = new WriteBatch();

        foreach (var datom in pendingTransaction.Data)
        {
            _writer.Reset();
            _writer.Advance(StackDatom.PaddingSize);
            datom.Explode(_registry, remapFn, ref stackDatom, _writer);
            stackDatom.T = thisTx.Value;
            stackDatom.PaddedSpan = _writer.GetWrittenSpanWritable();
            stackDatom.V = stackDatom.PaddedSpan.SliceFast(StackDatom.PaddingSize);

            _eatvHistory.Add(batch, ref stackDatom);
            _eatvCurrent.Add(batch, ref stackDatom);
            _aetvCurrent.Add(batch, ref stackDatom);
        }

        _db.Write(batch);
        _asOfTxId = thisTx;
        pendingTransaction.AssignedTxId = thisTx;
        node = [];

    }

    #endregion
}
