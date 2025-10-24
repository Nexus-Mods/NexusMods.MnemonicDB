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
    /// access to a portion of the index. If totalOrdered is true, the iterator will not be constructed in a way
    /// that filters out data outside of the initial seq prefix. Most of the time this should be false, as it greatly
    /// improves performance. It will need to be true if an entire index is being iterated over.
    /// </summary>
    [MustDisposeResource]
    public TRefEnumerator GetRefDatomEnumerator();
    
}
