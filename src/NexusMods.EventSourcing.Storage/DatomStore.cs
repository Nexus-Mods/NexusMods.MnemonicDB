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
    private readonly Subject<(TxId TxId, IDataChunk Datoms)> _updatesSubject;


    public DatomStore(ILogger<DatomStore> logger, NodeStore nodeStore, AttributeRegistry registry)
    {
        _logger = logger;
        _nodeStore = nodeStore;
        _registry = registry;
        _pooledWriter = new PooledMemoryBufferWriter();
        _nextEntId = EntityId.From(Ids.MinId(Ids.Partition.Entity) + 1);

        _updatesSubject = new Subject<(TxId TxId, IDataChunk Datoms)>();

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
                Log(pendingTransaction, out var chunk);

                await UpdateInMemoryIndexes(chunk, pendingTransaction.AssignedTxId!.Value);

                pendingTransaction.CompletionSource.SetResult(pendingTransaction.AssignedTxId.Value);
                _updatesSubject.OnNext((pendingTransaction.AssignedTxId.Value, chunk));
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
            var _ = await Transact(BuiltInAttributes.InitialDatoms);

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to bootstrap the datom store");
        }

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

    public IObservable<(TxId TxId, IDataChunk Datoms)> TxLog => _updatesSubject;

    private async Task UpdateInMemoryIndexes(IDataChunk chunk, TxId newTx)
    {
        _indexes = _indexes.Update(chunk, newTx);

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
        var offset = BinarySearch.SeekEqualOrLess(index.InMemory, index.Comparator, 0, index.InMemory.Length, startDatom);

        var lastEntity = EntityId.From(0);
        for (var idx = offset; idx < index.InMemory.Length; idx++)
        {
            var datom = index.InMemory[idx];
            if (datom.A != attr) break;
            if (datom.T > txId) continue;

            if (datom.E != lastEntity)
            {
                lastEntity = datom.E;
                yield return datom;
            }
        }
    }

    public IEnumerable<Datom> Where(TxId txId, EntityId id)
    {
        var index = _indexes.EAVT;

        var startDatom = new Datom
        {
            E = id,
            A = AttributeId.From(0),
            T = TxId.MaxValue,
            F = DatomFlags.Added,
        };
        var offset = BinarySearch.SeekEqualOrLess(index.InMemory, index.Comparator, 0, index.InMemory.Length, startDatom);

        var lastAttr = AttributeId.From(0);

        for (var idx = offset; idx < index.InMemory.Length; idx++)
        {
            var datom = index.InMemory[idx];
            if (datom.E != id) break;
            if (datom.T > txId) continue;

            if (datom.A != lastAttr)
            {
                lastAttr = datom.A;
                yield return datom;
            }

            yield return datom;
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
        var attr = _registry.GetAttributeId<TAttribute>();

        var index = _indexes.AVTE;

        var value = new byte[8];
        MemoryMarshal.Write(value.AsSpan(), id);

        var startDatom = new Datom
        {
            E = EntityId.From(UInt64.MaxValue),
            A = attr,
            T = TxId.MaxValue,
            V = value,
            F = DatomFlags.Added,
        };
        var offset = BinarySearch.SeekEqualOrLess(index.InMemory, index.Comparator, 0, index.InMemory.Length, startDatom);

        for (var idx = offset; idx < index.InMemory.Length; idx++)
        {
            var datom = index.InMemory[idx];
            if (datom.A != attr) break;

            var vValue = MemoryMarshal.Read<EntityId>(datom.V.Span);
            if (vValue != id) break;

            if (datom.T > txId) continue;

            yield return datom.E;

        }

    }

    #region Internals


    private void Log(PendingTransaction pendingTransaction, out IDataChunk chunk)
    {
        var newChunk = new AppendableChunk();
        foreach (var datom in pendingTransaction.Data)
            datom.Append(_registry, newChunk);

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

        newChunk.SetTx(nextTx);
        newChunk.RemapEntities(MaybeRemap, _registry);

        newChunk.Sort(_comparatorTxLog);

        chunk = newChunk.Pack();
        var newTxBlock = _nodeStore.LogTx(chunk);
        Debug.Assert(newTxBlock.Value == nextTxBlock.Value, "newTxBlock == nextTxBlock");
        pendingTransaction.AssignedTxId = nextTx;

    }

    #endregion
}
