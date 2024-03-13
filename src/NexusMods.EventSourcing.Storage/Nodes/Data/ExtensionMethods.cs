using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Abstractions.Columns.BlobColumns;

namespace NexusMods.EventSourcing.Storage.Nodes.Data;

public static class ExtensionMethods
{
    public static IReadable Sort(this IReadable readable, IDatomComparator comparator)
    {

    }

    /// <summary>
    /// Returns the indices that would sort the <see cref="IReadable"/> according to the given <see cref="IDatomComparator"/>.
    /// </summary>
    public static int[] GetSortIndices(this IReadable readable, IDatomComparator comparator)
    {

    }

}
