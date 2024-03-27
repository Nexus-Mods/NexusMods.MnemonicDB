using System.Collections.Immutable;

namespace NexusMods.EventSourcing.Storage.InMemoryBackend;

public interface IInMemoryIndex
{
    public ImmutableSortedSet<byte[]> Set { get; }
}
