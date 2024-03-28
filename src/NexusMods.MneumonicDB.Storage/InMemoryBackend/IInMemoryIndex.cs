using System.Collections.Immutable;

namespace NexusMods.MneumonicDB.Storage.InMemoryBackend;

public interface IInMemoryIndex
{
    public ImmutableSortedSet<byte[]> Set { get; }
}
