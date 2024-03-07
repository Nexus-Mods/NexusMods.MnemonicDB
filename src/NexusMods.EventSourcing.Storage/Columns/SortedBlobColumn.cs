using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using NexusMods.EventSourcing.Abstractions;

namespace NexusMods.EventSourcing.Storage.Columns;

/// <summary>
/// A lightweight column that represents a sorted view of another column, this is most often used
/// as a temporary view of a column before it is merged into another node.
/// </summary>
/// <param name="indexes"></param>
/// <param name="inner"></param>
public class SortedBlobColumn(int[] indexes, IBlobColumn inner) : IBlobColumn
{
    public IEnumerator<ReadOnlyMemory<byte>> GetEnumerator()
    {
        for (var i = 0; i < indexes.Length; i++)
        {
            yield return inner[indexes[i]];
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public ReadOnlyMemory<byte> this[int idx] => inner[indexes[idx]];

    public int Length => indexes.Length;
    public IBlobColumn Pack()
    {
        throw new NotSupportedException();
    }

    public void WriteTo<TWriter>(TWriter writer) where TWriter : IBufferWriter<byte>
    {
        throw new NotSupportedException();
    }
}
