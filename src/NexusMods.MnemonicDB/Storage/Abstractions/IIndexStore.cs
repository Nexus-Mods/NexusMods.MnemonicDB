using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.MnemonicDB.Storage.Abstractions;

public interface IIndexStore
{
    /// <summary>
    /// Get the type of the index
    /// </summary>
    IndexType Type { get; }

}
