using System;

namespace NexusMods.MnemonicDB.Abstractions;

public interface ILowLevelIterator
{
    public void SeekTo(scoped ReadOnlySpan<byte> span);

    public void SeekToPrev(ReadOnlySpan<byte> span);
    
    public void Next();
    
    public void Prev();
}
