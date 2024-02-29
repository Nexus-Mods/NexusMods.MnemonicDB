using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
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


    public DatomStore(ILogger<DatomStore> logger, NodeStore nodeStore, AttributeRegistry registry)
    {
        _logger = logger;
        _nodeStore = nodeStore;
        _registry = registry;
        _pooledWriter = new PooledMemoryBufferWriter();

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
            var value = await _txChannel.Reader.ReadAsync();
            try
            {
                if (value.Data.Length == 0)
                {
                    value.CompletionSource.SetResult(_indexes.AsOfTxId);
                    continue;
                }

                _logger.LogDebug("Processing transaction with {DatomCount} datoms", value.Data.Length);
                sw.Restart();
                var tx = Log(value.Data, out var chunk);

                await UpdateInMemoryIndexes(chunk, tx);

                value.CompletionSource.SetResult(tx);
                _logger.LogDebug("Transaction {TxId} processed in {Elapsed}ms, new in-memory size is {Count} datoms", tx, sw.ElapsedMilliseconds, _indexes.InMemorySize);
            }
            catch (Exception ex)
            {
                value.CompletionSource.TrySetException(ex);
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
        return await Transact(Enumerable.Empty<IWriteDatom>());
    }

    public async Task<TxId> Transact(IEnumerable<IWriteDatom> datoms)
    {
        var pending = new PendingTransaction { Data = datoms.ToArray() };
        if (!_txChannel.Writer.TryWrite(pending))
            throw new InvalidOperationException("Failed to write to the transaction channel");

        return await pending.CompletionSource.Task;
    }

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

    public Task<TxId> RegisterAttributes(IEnumerable<DbAttribute> newAttrs)
    {
        var datoms = new List<IWriteDatom>();

        foreach (var attr in newAttrs)
        {
            datoms.Add(BuiltInAttributes.UniqueId.Assert(EntityId.From(attr.AttrEntityId.Value), attr.UniqueId));
            datoms.Add(BuiltInAttributes.ValueSerializerId.Assert(EntityId.From(attr.AttrEntityId.Value), attr.ValueTypeId));
        }

        return Transact(datoms);
    }

    public Expression GetValueReadExpression(Type attribute, Expression valueSpan, out ulong attributeId)
    {
        throw new NotImplementedException();
    }

    public IEnumerable<EntityId> ReverseLookup<TAttribute>(TxId txId) where TAttribute : IAttribute<EntityId>
    {
        throw new NotImplementedException();
    }

    #region Internals


    public TxId Log(IEnumerable<IWriteDatom> input, out IDataChunk chunk)
    {
        var newChunk = new AppendableChunk();
        foreach (var datom in input)
            datom.Append(_registry, newChunk);

        var nextTxBlock = _nodeStore.GetNextTx();

        var nextTx = TxId.From(Ids.MakeId(Ids.Partition.Tx, nextTxBlock.Value));

        newChunk.SetTx(nextTx);

        newChunk.Sort(_comparatorTxLog);

        var newTxBlock = _nodeStore.LogTx(newChunk);
        Debug.Assert(newTxBlock == nextTxBlock, "newTxBlock == nextTxBlock");
        chunk = newChunk;
        return nextTx;
    }

    #endregion
}
