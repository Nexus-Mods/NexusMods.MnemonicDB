using System;

namespace NexusMods.MnemonicDB.Abstractions;

public interface ILowLevelIterator : IDisposable
{
    /// <summary>
    /// Move the iterator to the first datom that matches the given span.
    /// </summary>
    /// <param name="span"></param>
    public void SeekTo(scoped ReadOnlySpan<byte> span);

    /// <summary>
    /// Move the iterator to the first datom before the given span.
    /// </summary>
    public void SeekToPrev(scoped ReadOnlySpan<byte> span);
    
    /// <summary>
    /// Move to the next datom
    /// </summary>
    /// <returns></returns>
    public void Next();
    
    /// <summary>
    /// Move to the previous datom
    /// </summary>
    public void Prev();
}
