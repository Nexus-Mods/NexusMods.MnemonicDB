using System.Runtime.CompilerServices;

namespace NexusMods.MnemonicDB.Abstractions.ElementComparers;

using System;
using System.Text;

internal static unsafe class Utf8Comparer
{
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static int Utf8CaseInsensitiveCompare(byte* ptrA, int lenA, byte* ptrB, int lenB)
    {
        int indexA = 0, indexB = 0;

        while (indexA < lenA && indexB < lenB)
        {
            var codePointA = DecodeUtf8CodePoint(ptrA, lenA, ref indexA);
            var codePointB = DecodeUtf8CodePoint(ptrB, lenB, ref indexB);

            var runeA = new Rune(codePointA);
            var runeB = new Rune(codePointB);

            var lowerRuneA = Rune.ToLowerInvariant(runeA);
            var lowerRuneB = Rune.ToLowerInvariant(runeB);

            var cmp = lowerRuneA.Value.CompareTo(lowerRuneB.Value);
            if (cmp != 0)
                return cmp;
        }

        if (indexA < lenA)
            return 1; // A is longer
        if (indexB < lenB)
            return -1; // B is longer

        return 0; // Sequences are equal
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private static uint DecodeUtf8CodePoint(byte* ptr, int len, ref int index)
    {
        if (index >= len)
            ThrowInvalidUTF8Sequence();

        byte firstByte = ptr[index];
        uint codePoint;
        int additionalBytes;

        if ((firstByte & 0b1000_0000) == 0)
        {
            // 1-byte sequence
            codePoint = firstByte;
            additionalBytes = 0;
        }
        else if ((firstByte & 0b1110_0000) == 0b1100_0000)
        {
            // 2-byte sequence
            codePoint = (uint)(firstByte & 0b0001_1111);
            additionalBytes = 1;
        }
        else if ((firstByte & 0b1111_0000) == 0b1110_0000)
        {
            // 3-byte sequence
            codePoint = (uint)(firstByte & 0b0000_1111);
            additionalBytes = 2;
        }
        else if ((firstByte & 0b1111_1000) == 0b1111_0000)
        {
            // 4-byte sequence
            codePoint = (uint)(firstByte & 0b0000_0111);
            additionalBytes = 3;
        }
        else
        {
            // Invalid UTF-8 sequence
            ThrowInvalidUTF8Sequence();
            return 0; // Unreachable
        }

        if (index + additionalBytes >= len)
            ThrowInvalidUTF8Sequence();

        for (int i = 1; i <= additionalBytes; i++)
        {
            index++;
            byte nextByte = ptr[index];
            if ((nextByte & 0b1100_0000) != 0b1000_0000)
                ThrowInvalidUTF8Sequence();

            codePoint = (codePoint << 6) | (uint)(nextByte & 0b0011_1111);
        }

        index++; // Move to the next character position

        return codePoint;
    }
    
    private static void ThrowInvalidUTF8Sequence()
    {
        throw new InvalidOperationException("Invalid UTF-8 sequence.");
    }
}
