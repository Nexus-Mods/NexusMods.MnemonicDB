using NexusMods.EventSourcing.Abstractions;

namespace NexusMods.EventSourcing.Storage.Abstractions;

public interface IIndexStore
{
    IDatomIterator GetIterator();
}
