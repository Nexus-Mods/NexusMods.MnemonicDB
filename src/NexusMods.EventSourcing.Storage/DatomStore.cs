using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reactive.Subjects;
using System.Runtime.InteropServices;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Storage.Algorithms;
using NexusMods.EventSourcing.Storage.DatomStorageStructures;
using NexusMods.EventSourcing.Storage.Nodes;
using NexusMods.EventSourcing.Storage.Sorters;

namespace NexusMods.EventSourcing.Storage;


public class DatomStore : IDatomStore
{
    private readonly AttributeRegistry _registry;
    private readonly PooledMemoryBufferWriter _pooledWriter;
    private readonly TxLog _comparatorTxLog;
    private readonly NodeStore _nodeStore;
    private DatomStoreState _indexes;
    private readonly ILogger<DatomStore> _logger;
    private readonly Channel<PendingTransaction> _txChannel;
    private EntityId _nextEntId;
    private readonly Subject<(TxId TxId, IDataNode Datoms)> _updatesSubject;
    private readonly DatomStoreSettings _settings;


    public DatomStore(ILogger<DatomStore> logger, NodeStore nodeStore, AttributeRegistry registry, DatomStoreSettings settings)
    {
        _logger = logger;
        _settings = settings;
        _nodeStore = nodeStore;
        _registry = registry;
        _pooledWriter = new PooledMemoryBufferWriter();
        _nextEntId = EntityId.From(Ids.MinId(Ids.Partition.Entity) + 1);

        _updatesSubject = new Subject<(TxId TxId, IDataNode Datoms)>();

        registry.Populate(BuiltInAttributes.Initial);

        _comparatorTxLog = new TxLog(_registry);

        _indexes = DatomStoreState.Empty(TxId.From(0), _registry);

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
                if (pendingTransaction.Data.Length == 0)
                {
                    pendingTransaction.AssignedTxId = _indexes.AsOfTxId;
                    pendingTransaction.CompletionSource.SetResult(_indexes.AsOfTxId);
                    continue;
                }

                _logger.LogDebug("Processing transaction with {DatomCount} datoms", pendingTransaction.Data.Length);
                sw.Restart();
                Log(pendingTransaction, out var node);

                await UpdateInMemoryIndexes(node, pendingTransaction.AssignedTxId!.Value);

                _updatesSubject.OnNext((pendingTransaction.AssignedTxId.Value, node));
                pendingTransaction.CompletionSource.SetResult(pendingTransaction.AssignedTxId.Value);
                _logger.LogDebug("Transaction {TxId} processed in {Elapsed}ms, new in-memory size is {Count} datoms", pendingTransaction.AssignedTxId!.Value, sw.ElapsedMilliseconds, _indexes.InMemorySize);
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

    }

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

    public TxId AsOfTxId => _indexes.AsOfTxId;

    public void Dispose()
    {
        _txChannel.Writer.Complete();
    }

    public async Task<TxId> Sync()
    {
        await Transact(Enumerable.Empty<IWriteDatom>());
        return _indexes.AsOfTxId;
    }

    public async Task<DatomStoreTransactResult> Transact(IEnumerable<IWriteDatom> datoms)
    {
        var pending = new PendingTransaction { Data = datoms.ToArray() };
        if (!_txChannel.Writer.TryWrite(pending))
            throw new InvalidOperationException("Failed to write to the transaction channel");

        await pending.CompletionSource.Task;

        return new DatomStoreTransactResult(pending.AssignedTxId!.Value, pending.Remaps);
    }

    public IObservable<(TxId TxId, IDataNode Datoms)> TxLog => _updatesSubject;

    private async Task UpdateInMemoryIndexes(AppendableNode node, TxId newTx)
    {
        _indexes = await _indexes.Update(node, newTx, _settings, _nodeStore, _logger);

    }

    public IEnumerable<Datom> Where<TAttr>(TxId txId) where TAttr : IAttribute
    {
        var attr = _registry.GetAttributeId<TAttr>();
        var index = _indexes.AEVT;


        var startDatom = new Datom
        {
            E = EntityId.From(0),
            A = attr,
            T = TxId.MaxValue,
            F = DatomFlags.Added,
        };

        var inMemory = WhereInner(index.InMemory, startDatom);
        var history = WhereInner(index.History, startDatom);
        var merged = history.Merge(inMemory, _indexes.AEVT.Comparator);

        var lastEntity = EntityId.From(0);
        foreach (var datom in merged)
        {
            if (datom.A != attr) break;
            if (datom.T > txId) continue;

            if (datom.E != lastEntity)
            {
                lastEntity = datom.E;
                yield return datom;
            }
        }
    }

    private IEnumerable<Datom> WhereInner(IDataNode node, Datom startDatom)
    {
        var offset = node.FindAETV(0, node.Length, startDatom, _registry);
        for (var idx = offset; idx < node.Length; idx++)
        {
            yield return node[idx];
        }
    }

    public IEnumerable<Datom> Where(TxId txId, EntityId id)
    {
        var index = _indexes.EAVT;

        var lastAttr = AttributeId.From(0);

        var inMemory = WhereInner(id, index.InMemory, _registry);
        var history = WhereInner(id, index.History, _registry);
        var merged = history.Merge(inMemory, _indexes.EAVT.Comparator);

        foreach (var datom in merged)
        {
            if (datom.E != id) break;
            if (datom.T > txId) continue;

            if (datom.A != lastAttr)
            {
                lastAttr = datom.A;
                yield return datom;
            }
        }
    }

    private static IEnumerable<Datom> WhereInner(EntityId id, IDataNode node, IAttributeRegistry registry)
    {
        var startDatom = new Datom
        {
            E = id,
            A = AttributeId.From(0),
            T = TxId.MaxValue,
            F = DatomFlags.Added,
        };
        var offset = node.FindEATV(0, node.Length, startDatom, registry);

        for (var idx = offset; idx < node.Length; idx++)
        {
            yield return node[idx];
        }
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
        var index = _indexes.AVTE;


        var inMemory = ReverseLookupForIndex<TAttribute>(txId, id, index.InMemory, index.Comparator);
        var history = ReverseLookupForIndex<TAttribute>(txId, id, index.History, index.Comparator);
        var merged = history.Merge(inMemory, _indexes.AVTE.Comparator);

        var attr = _registry.GetAttributeId<TAttribute>();

        foreach (var datom in merged)
        {
            if (datom.A != attr) break;

            var vValue = MemoryMarshal.Read<EntityId>(datom.V.Span);
            if (vValue != id) break;

            if (datom.T > txId) continue;

            yield return datom.E;
        }
    }


    private IEnumerable<Datom> ReverseLookupForIndex<TAttribute>(TxId txId, EntityId id, IDataNode node, IDatomComparator comparator)
        where TAttribute : IAttribute<EntityId>
    {
        var attr = _registry.GetAttributeId<TAttribute>();

        var value = new byte[8];
        MemoryMarshal.Write(value.AsSpan(), id);

        var startDatom = new Datom
        {
            E = EntityId.From(ulong.MaxValue),
            A = attr,
            T = TxId.MaxValue,
            V = value,
            F = DatomFlags.Added,
        };
        var offset = node.FindAVTE(0, node.Length, startDatom, _registry);

        for (var idx = offset; idx < node.Length; idx++)
        {
            yield return node[idx];
        }
    }

    #region Internals


    private void Log(PendingTransaction pendingTransaction, out AppendableNode node)
    {
        var newNode = new AppendableNode();
        foreach (var datom in pendingTransaction.Data)
            datom.Append(_registry, newNode);

        var nextTxBlock = _nodeStore.GetNextTx();

        var nextTx = TxId.From(Ids.MakeId(Ids.Partition.Tx, nextTxBlock.Value));

        EntityId MaybeRemap(EntityId id)
        {
            if (Ids.GetPartition(id) == Ids.Partition.Tmp)
            {
                if (!pendingTransaction.Remaps.TryGetValue(id, out var newId))
                {
                    if (id.Value == Ids.MinId(Ids.Partition.Tmp))
                    {
                        var remapTo = EntityId.From(nextTx.Value);
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

        newNode.SetTx(nextTx);
        newNode.RemapEntities(MaybeRemap, _registry);

        newNode.Sort(_comparatorTxLog);

        node = newNode;
        var newTxBlock = _nodeStore.LogTx(newNode.Pack());
        Debug.Assert(newTxBlock.Value == nextTxBlock.Value, "newTxBlock == nextTxBlock");
        pendingTransaction.AssignedTxId = nextTx;

    }

    #endregion
}
