using System;
using System.Runtime.CompilerServices;
using System.Text;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace NexusMods.MnemonicDB.Abstractions.ElementComparers;

public static unsafe class Utf8Comparer
{
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static int Utf8CaseInsensitiveCompare(byte* ptrA, int lenA, byte* ptrB, int lenB)
    {
        int i = 0;
        int minLength = Math.Min(lenA, lenB);

        // byte-by-byte fast path: only skip pure ASCII
        while (i < minLength)
        {
            byte a = ptrA[i], b = ptrB[i];

            // both ASCII?
            if (a < 0x80 && b < 0x80)
            {
                if (a == b)
                {
                    i++;
                    continue;
                }

                // case-insensitive ASCII compare
                byte aUp = (byte)(a & 0xDF);
                byte bUp = (byte)(b & 0xDF);
                if (aUp != bUp)
                    return aUp < bUp ? -1 : 1;

                i++;
                continue;
            }

            // as soon as we hit ANY non-ASCII, stop here and
            // let the full code-point decoder handle it
            break;
        }

        // fallback: decode full code points from i onward
        int indexA = i, indexB = i;
        while (indexA < lenA && indexB < lenB)
        {
            uint cpA = DecodeUtf8CodePoint(ptrA, lenA, ref indexA);
            uint cpB = DecodeUtf8CodePoint(ptrB, lenB, ref indexB);

            if (cpA < 128 && cpB < 128)
            {
                byte ua = (byte)(cpA & 0xDF);
                byte ub = (byte)(cpB & 0xDF);
                if (ua != ub)
                    return ua < ub ? -1 : 1;
            }
            else
            {
                var rA = Rune.ToUpperInvariant(new Rune(cpA));
                var rB = Rune.ToUpperInvariant(new Rune(cpB));
                int c = rA.Value.CompareTo(rB.Value);
                if (c != 0) return c;
            }
        }

        if (indexA < lenA) return 1;
        if (indexB < lenB) return -1;
        return 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private static uint DecodeUtf8CodePoint(byte* ptr, int len, ref int index)
    {
        byte firstByte = ptr[index];
        uint codePoint;
        int additionalBytes;

        if ((firstByte & 0b1000_0000) == 0)
        {
            codePoint = firstByte;
            additionalBytes = 0;
        }
        else if ((firstByte & 0b1110_0000) == 0b1100_0000)
        {
            codePoint = (uint)(firstByte & 0b0001_1111);
            additionalBytes = 1;
        }
        else if ((firstByte & 0b1111_0000) == 0b1110_0000)
        {
            codePoint = (uint)(firstByte & 0b0000_1111);
            additionalBytes = 2;
        }
        else if ((firstByte & 0b1111_1000) == 0b1111_0000)
        {
            codePoint = (uint)(firstByte & 0b0000_0111);
            additionalBytes = 3;
        }
        else
        {
            ThrowInvalidUTF8Sequence();
            return 0;
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

        index++;
        return codePoint;
    }

    private static void ThrowInvalidUTF8Sequence()
    {
        throw new InvalidOperationException("Invalid UTF-8 sequence.");
    }
}
