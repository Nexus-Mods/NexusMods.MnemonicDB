using System.Collections.Immutable;

namespace NexusMods.MnemonicDB.Storage.InMemoryBackend;

public interface IInMemoryIndex
{
    public ImmutableSortedSet<byte[]> Set { get; }
}
