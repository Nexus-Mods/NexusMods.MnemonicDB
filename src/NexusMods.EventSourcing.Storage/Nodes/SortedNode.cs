using System;
using System.Collections.Generic;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Abstractions.Columns.BlobColumns;

namespace NexusMods.EventSourcing.Storage.Nodes;

/// <summary>
/// Represents a sorted view of another node, this is most often used as a temporary view of a
/// node before it is merged into another node.
/// </summary>
/// <param name="indexes"></param>
/// <param name="inner"></param>
public class SortedNode(int[] indexes, IReadable inner) : IReadable
{
    public int Count { get; }

    public ReadOnlySpan<byte> this[int idx] => throw new NotImplementedException();

    public IUnpacked Unpack()
    {
        throw new NotImplementedException();
    }
}
