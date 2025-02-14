namespace NexusMods.MnemonicDB.Abstractions.UnsafeIterators;

public unsafe interface IUnsafeDatom
{
    public byte* Key { get; }
    public int KeySize { get; }
}
