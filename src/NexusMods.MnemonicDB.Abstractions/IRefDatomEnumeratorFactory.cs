using JetBrains.Annotations;

namespace NexusMods.MnemonicDB.Abstractions;

/// <summary>
/// A snapshot that returns a specific type of low-level iterator.
/// </summary>
public interface IRefDatomEnumeratorFactory<out TRefEnumerator>
    where TRefEnumerator : IRefDatomEnumerator
{
    /// <summary>
    /// Get a low-level iterator for this snapshot, this can be combined with slice descriptors to get high performance
    /// access to a portion of the index
    /// </summary>
    [MustDisposeResource]
    public TRefEnumerator GetRefDatomEnumerator();
}
