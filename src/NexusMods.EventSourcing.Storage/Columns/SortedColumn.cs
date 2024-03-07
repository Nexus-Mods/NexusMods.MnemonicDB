using System;
using System.Buffers;
using NexusMods.EventSourcing.Abstractions;

namespace NexusMods.EventSourcing.Storage.Columns;

/// <summary>
/// A lightweight column that represents a sorted view of another column, this is most often
/// used as a temporary view of a column before it is merged into another node.
/// </summary>
/// <param name="indexes"></param>
/// <param name="inner"></param>
/// <typeparam name="T"></typeparam>
public class SortedColumn<T>(int[] indexes, IColumn<T> inner) : IColumn<T>
{
    public T this[int index] => inner[indexes[index]];

    public int Length => indexes.Length;
    public IColumn<T> Pack()
    {
        throw new NotSupportedException();
    }

    public void WriteTo<TWriter>(TWriter writer) where TWriter : IBufferWriter<byte>
    {
        throw new NotImplementedException();
    }

    public void CopyTo(Span<T> destination)
    {
        for (var i = 0; i < indexes.Length; i++)
            destination[i] = inner[indexes[i]];
    }
}
