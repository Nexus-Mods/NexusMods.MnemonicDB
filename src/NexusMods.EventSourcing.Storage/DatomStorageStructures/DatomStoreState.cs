using System;
using System.Buffers;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Storage.Nodes;

namespace NexusMods.EventSourcing.Storage.DatomStorageStructures;

/// <summary>
/// A single immutable containing class for the state of the index, this
/// is replaced on the DatomStore with every transaction to reflect the new state
/// of the indexes.
/// </summary>
public record DatomStoreState
{
    /// <summary>
    /// The in-memory size of the index in datoms.
    /// </summary>
    public required int InMemorySize { get; init; }
    public required TxId AsOfTxId { get; init; }

    public required TxId LastFlushedTxId { get; init; }
    public required IndexRoot EAVT { get; init; }
    public required IndexRoot AEVT { get; init; }
    public required IndexRoot AVTE { get; init; }

    public static DatomStoreState Empty(TxId id, AttributeRegistry registry)
    {
        /*
        return new DatomStoreState
        {
            InMemorySize = 0,
            LastFlushedTxId = TxId.MinValue,
            AsOfTxId = id,
            EAVT = IndexRoot.Empty(SortOrders.EATV, registry),
            AEVT = IndexRoot.Empty(SortOrders.AETV, registry),
            AVTE = IndexRoot.Empty(SortOrders.AVTE, registry)
        };
        */
        throw new NotImplementedException();

    }

    public void WriteTo<TBufferWriter>(TBufferWriter writer)
        where TBufferWriter : IBufferWriter<byte>
    {
        throw new NotImplementedException();
    }

    /*
    public static DatomStoreState ReadFrom(in BufferReader reader, AttributeRegistry registry, NodeStore store)
    {
        var lastFlushedId = reader.Read<TxId>();
        var eavtKey = reader.Read<StoreKey>();
        var aevtKey = reader.Read<StoreKey>();
        var avteKey = reader.Read<StoreKey>();


        return new DatomStoreState
        {
            InMemorySize = 0,
            AsOfTxId = lastFlushedId,
            LastFlushedTxId = lastFlushedId,
            EAVT = new IndexRoot
            {
                SortOrder = SortOrders.EATV,
                Comparator = registry.CreateComparator(SortOrders.EATV),
                InMemory = new AppendableNode(),
                History = new ReferenceIndexNode(store, eavtKey, null)
            },
            AEVT = new IndexRoot
            {
                SortOrder = SortOrders.AETV,
                Comparator = registry.CreateComparator(SortOrders.AETV),
                InMemory = new AppendableNode(),
                History = new ReferenceIndexNode(store, aevtKey, null)
            },
            AVTE = new IndexRoot
            {
                SortOrder = SortOrders.AVTE,
                Comparator = registry.CreateComparator(SortOrders.AVTE),
                InMemory = new AppendableNode(),
                History = new ReferenceIndexNode(store, avteKey, null)
            }
        };
    }

    public async ValueTask<DatomStoreState> Update(AppendableNode node, TxId newTx, DatomStoreSettings settings, NodeStore nodeStore, ILogger logger)
    {
        var newState = this with
        {
            InMemorySize = InMemorySize + node.Length,
            AsOfTxId = newTx,
            EAVT = EAVT with { InMemory = EAVT.Update(node) },
            AEVT = AEVT with { InMemory = AEVT.Update(node) },
            AVTE = AVTE with { InMemory = AVTE.Update(node) }
        };

        if (newState.InMemorySize > settings.MaxInMemoryDatoms)
        {
            var sw = Stopwatch.StartNew();
            logger.LogDebug("Flushing in-memory indexes to history");

            var newEatv = newState.EAVT.FlushMemoryToHistory(nodeStore);
            var newAetv = newState.AEVT.FlushMemoryToHistory(nodeStore);
            var newAvte = newState.AVTE.FlushMemoryToHistory(nodeStore);

            newState = newState with
            {
                InMemorySize = 0,
                LastFlushedTxId = newTx,
                EAVT = await newEatv,
                AEVT = await newAetv,
                AVTE = await newAvte
            };

            nodeStore.PutRoot(newState);
            logger.LogInformation("Flushed in-memory indexes to history in {ElapsedMilliseconds}ms", sw.ElapsedMilliseconds);
        }

        return newState;
    }
    */

}
