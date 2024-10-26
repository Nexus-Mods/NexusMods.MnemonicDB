using System;

namespace NexusMods.MnemonicDB.ManagedTree.Abstractions;

public interface IIterator
{
    public ReadOnlySpan<byte> Current { get; }
    
    /// <summary>
    ///  Set the iterator to the first key in the store
    /// </summary>
    public void Start();
    
    /// <summary>
    /// Set the iterator to the last key in the store
    /// </summary>
    public void End();
    
    public bool MoveNext();
    
    public bool MovePrev();
    
    public bool Seek(ReadOnlySpan<byte> key);
}
