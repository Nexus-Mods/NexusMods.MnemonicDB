using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Storage.Nodes;

namespace NexusMods.EventSourcing.Storage.DatomStorageStructures;

/// <summary>
/// Root node of an index, contains links to the current and history indexes, as well
/// as the current in-memory index.
/// </summary>
public record IndexRoot
{
    /// <summary>
    /// The sort order of the index.
    /// </summary>
    public required SortOrders SortOrder { get; init; }

    /// <summary>
    /// The comparator used to sort the index.
    /// </summary>
    public required IDatomComparator Comparator { get; init; }

    /// <summary>
    /// The current in-memory index.
    /// </summary>
    public required IDataChunk InMemory { get; init; }

    public static IndexRoot Empty(SortOrders order, AttributeRegistry registry)
    {
        return new IndexRoot
        {
            SortOrder = order,
            Comparator = registry.CreateComparator(order),
            InMemory = new AppendableChunk()
        };
    }

    public IDataChunk Update(IDataChunk chunk)
    {
        var newChunk = AppendableChunk.Initialize(InMemory);
        newChunk.Append(chunk);
        newChunk.Sort(Comparator);
        var packed = newChunk.Pack();
        return packed;
    }
}
