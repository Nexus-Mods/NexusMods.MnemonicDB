using System.Collections.Immutable;
using NexusMods.EventSourcing.Abstractions;

namespace NexusMods.EventSourcing.Storage.Abstractions;

public interface IIndexStore
{
    IndexType Type { get; }
    IDatomIterator GetIterator();
}
