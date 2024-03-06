using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NexusMods.EventSourcing.Abstractions;

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
    public required IndexRoot EAVT { get; init; }
    public required IndexRoot AEVT { get; init; }
    public required IndexRoot AVTE { get; init; }

    public static DatomStoreState Empty(TxId id, AttributeRegistry registry)
    {
        return new DatomStoreState
        {
            InMemorySize = 0,
            AsOfTxId = id,
            EAVT = IndexRoot.Empty(SortOrders.EATV, registry),
            AEVT = IndexRoot.Empty(SortOrders.AETV, registry),
            AVTE = IndexRoot.Empty(SortOrders.AVTE, registry)
        };

    }

    public async ValueTask<DatomStoreState> Update(IDataNode node, TxId newTx, DatomStoreSettings settings, INodeStore nodeStore, ILogger logger)
    {
        var newState = new DatomStoreState
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
                EAVT = await newEatv,
                AEVT = await newAetv,
                AVTE = await newAvte
            };
            logger.LogInformation("Flushed in-memory indexes to history in {ElapsedMilliseconds}ms", sw.ElapsedMilliseconds);
        }

        return newState;
    }

}
