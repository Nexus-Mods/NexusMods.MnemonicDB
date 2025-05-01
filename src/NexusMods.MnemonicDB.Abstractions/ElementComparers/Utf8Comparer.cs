using System;
using System.Runtime.CompilerServices;
using System.Text;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace NexusMods.MnemonicDB.Abstractions.ElementComparers;

internal static unsafe class Utf8Comparer
{
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static int Utf8CaseInsensitiveCompare(byte* ptrA, int lenA, byte* ptrB, int lenB)
    {
        int i = 0;
        int minLength = Math.Min(lenA, lenB);
        
        if (Avx2.IsSupported)
        {
            Vector256<byte> caseMask = Vector256.Create((byte)0xDF);
            var asciiThreshold = Vector256.Create((sbyte)0x7F);

            while (i + 32 <= minLength)
            {
                var va = Avx.LoadVector256(ptrA + i);
                var vb = Avx.LoadVector256(ptrB + i);

                var combined = Avx2.Or(va, vb);
                var isNonAscii = Avx2.CompareGreaterThan(combined.AsSByte(), asciiThreshold);
                if (Avx2.MoveMask(isNonAscii.AsByte()) != 0)
                    break;

                var aMasked = Avx2.And(va, caseMask);
                var bMasked = Avx2.And(vb, caseMask);

                var cmp = Avx2.CompareEqual(aMasked, bMasked);
                if (Avx2.MoveMask(cmp) != -1)
                    break;

                i += 32;
            }
        }
        else if (Sse2.IsSupported)
        {
            Vector128<byte> caseMask = Vector128.Create((byte)0xDF);
            var asciiThreshold = Vector128.Create((sbyte)0x7F);

            while (i + 16 <= minLength)
            {
                var va = Sse2.LoadVector128(ptrA + i);
                var vb = Sse2.LoadVector128(ptrB + i);

                var combined = Sse2.Or(va, vb);
                var isNonAscii = Sse2.CompareGreaterThan(combined.AsSByte(), asciiThreshold);
                if (Sse2.MoveMask(isNonAscii.AsByte()) != 0)
                    break;

                var aMasked = Sse2.And(va, caseMask);
                var bMasked = Sse2.And(vb, caseMask);

                var cmp = Sse2.CompareEqual(aMasked, bMasked);
                if (Sse2.MoveMask(cmp) != 0xFFFF)
                    break;

                i += 16;
            }
        }

        // Byte-by-byte loop
        while (i < lenA && i < lenB)
        {
            byte a = ptrA[i], b = ptrB[i];
            if (a == b)
            {
                i++;
                continue;
            }

            if (a < 128 && b < 128)
            {
                byte aUp = (byte)(a & 0xDF);
                byte bUp = (byte)(b & 0xDF);
                if (aUp != bUp)
                    return aUp < bUp ? -1 : 1;

                i++;
                continue;
            }

            // Non-ASCII fallback
            break;
        }

        int indexA = i, indexB = i;
        while (indexA < lenA && indexB < lenB)
        {
            var codePointA = DecodeUtf8CodePoint(ptrA, lenA, ref indexA);
            var codePointB = DecodeUtf8CodePoint(ptrB, lenB, ref indexB);

            if (codePointA < 128 && codePointB < 128)
            {
                byte upperA = (byte)(codePointA & 0xDF);
                byte upperB = (byte)(codePointB & 0xDF);
                if (upperA != upperB)
                    return upperA < upperB ? -1 : 1;
            }
            else
            {
                var normA = Rune.ToUpperInvariant(new Rune(codePointA));
                var normB = Rune.ToUpperInvariant(new Rune(codePointB));
                int cmp = normA.Value.CompareTo(normB.Value);
                if (cmp != 0)
                    return cmp;
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
