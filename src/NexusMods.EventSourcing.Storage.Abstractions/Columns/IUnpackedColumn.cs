using System;

namespace NexusMods.EventSourcing.Storage.Abstractions.Columns;

public interface IUnpackedColumn<T> : IColumn<T>
{
    public ReadOnlySpan<T> Data { get; }
}
