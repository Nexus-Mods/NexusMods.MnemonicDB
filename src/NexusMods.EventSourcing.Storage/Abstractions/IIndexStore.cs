using System.Collections.Immutable;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Abstractions.DatomIterators;

namespace NexusMods.EventSourcing.Storage.Abstractions;

public interface IIndexStore
{
    IndexType Type { get; }
    IDatomSource GetIterator();
}
