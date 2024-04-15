namespace NexusMods.MnemonicDB.Abstractions.ElementComparers;

/// <summary>
/// Compares the A part of the key.
/// </summary>
public class AComparer : IElementComparer
{
    /// <inheritdoc />
    public static unsafe int Compare(byte* aPtr, int aLen, byte* bPtr, int bLen)
    {
        return IElementComparer.KeyPrefix(aPtr)->A.CompareTo(IElementComparer.KeyPrefix(bPtr)->A);
    }
}
