using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Reloaded.Memory.Extensions;

namespace NexusMods.MnemonicDB.Abstractions;

public class AValueSerializer
{
    #region Constants
    private const int MaxStackAlloc = 128;
    private static readonly Encoding AsciiEncoding = Encoding.ASCII;
    private static readonly Encoding Utf8Encoding = Encoding.UTF8;
    #endregion


    /// <summary>
    ///     Performs a highly optimized, sort between two value pointers.
    /// </summary>
    /// <param name="a"></param>
    /// <param name="alen"></param>
    /// <param name="b"></param>
    /// <param name="blen"></param>
    /// <returns></returns>
    public static unsafe int CompareUnsafe(byte* a, int alen, byte* b, int blen)
    {
        var typeA = a[0];
        var typeB = b[0];

        if (typeA != typeB)
            return typeA.CompareTo(typeB);

        var minLength = Math.Min(alen, blen);
        var result = Unsafe.AsRef<int>(a) - Unsafe.AsRef<int>(b);
        if (result != 0)
            return result;

        for (var i = sizeof(int); i < minLength; i++)
        {
            result = a[i] - b[i];
            if (result != 0)
                return result;
        }

        return alen - blen;
    }


}
