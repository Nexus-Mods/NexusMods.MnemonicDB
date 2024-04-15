namespace NexusMods.MnemonicDB.Abstractions.ElementComparers;

/// <summary>
/// Compares the assert part of the datom
/// </summary>
public class AssertComparer: IElementComparer
{
    public static unsafe int Compare(byte* aPtr, int aLen, byte* bPtr, int bLen)
    {
        return IElementComparer.KeyPrefix(aPtr)->IsRetract.CompareTo(IElementComparer.KeyPrefix(bPtr)->IsRetract);
    }
}
