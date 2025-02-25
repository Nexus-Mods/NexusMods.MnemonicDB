namespace NexusMods.MnemonicDB.Abstractions;

/// <summary>
/// A collection that can be enumerated as ref datom enumerator
/// </summary>
public interface IRefDatomEnumerable<out T> 
    where T : IRefDatomEnumerator, allows ref struct
{
    /// <summary>
    /// Create a new enumerator
    /// </summary>
    public T GetEnumerator();
}
