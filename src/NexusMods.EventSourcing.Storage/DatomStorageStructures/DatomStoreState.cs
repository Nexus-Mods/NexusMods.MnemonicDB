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

    public static DatomStoreState Empty(TxId id, AttributeRegistry registry)
    {
        return new DatomStoreState
        {
            InMemorySize = 0,
            AsOfTxId = id,
            EAVT = IndexRoot.Empty(SortOrders.EATV, registry),
            AEVT = IndexRoot.Empty(SortOrders.AETV, registry)
        };

    }

    public DatomStoreState Update(IDataChunk chunk, TxId newTx)
    {
        return new DatomStoreState
        {
            InMemorySize = InMemorySize + chunk.Length,
            AsOfTxId = newTx,
            EAVT = EAVT with { InMemory = EAVT.Update(chunk) },
            AEVT = AEVT with { InMemory = AEVT.Update(chunk) }
        };
    }
}
