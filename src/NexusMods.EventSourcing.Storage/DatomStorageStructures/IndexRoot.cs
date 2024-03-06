using System.Threading.Tasks;
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
    public required IDataNode InMemory { get; init; }

    /// <summary>
    /// The history index
    /// </summary>
    public required IIndexNode History { get; init; }

    public static IndexRoot Empty(SortOrders order, AttributeRegistry registry)
    {
        return new IndexRoot
        {
            SortOrder = order,
            Comparator = registry.CreateComparator(order),
            InMemory = new AppendableNode(),
            History = new AppendableIndexNode(registry.CreateComparator(order))
        };
    }

    public IDataNode Update(IDataNode node)
    {
        var newChunk = AppendableNode.Initialize(InMemory);
        newChunk.Append(node);
        newChunk.Sort(Comparator);
        var packed = newChunk.Pack();
        return packed;
    }

    public Task<IndexRoot> FlushMemoryToHistory(INodeStore store)
    {
        return Task.Run(() =>
        {
            var newHistory = AppendableIndexNode.UnpackFrom(History)
                .Ingest(InMemory);
            var flushed = newHistory.Flush(store);
            return this with { InMemory = new AppendableNode(), History = (IIndexNode)flushed };
        });
    }
}
