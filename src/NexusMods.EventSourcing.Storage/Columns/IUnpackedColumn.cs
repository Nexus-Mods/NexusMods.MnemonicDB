using System;
using NexusMods.EventSourcing.Abstractions;

namespace NexusMods.EventSourcing.Storage.Abstractions.Columns;

public interface IUnpackedColumn<T> : IColumn<T>
{
    public ReadOnlySpan<T> Data { get; }
}
