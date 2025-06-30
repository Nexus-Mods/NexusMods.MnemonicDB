using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace NexusMods.MnemonicDB.QueryV2;


[StructLayout(LayoutKind.Sequential, Pack = 1, Size = 16)]
public unsafe struct DuckDBStringValuePtr
{
    public uint Length;
    public fixed sbyte Prefix[4];
    public byte* Ptr;
}

[StructLayout(LayoutKind.Sequential, Pack = 1, Size = 16)]
public unsafe struct DuckDBStringValueInlined
{
    public uint Length;
    public fixed sbyte Inlined[12];
}

[StructLayout(LayoutKind.Explicit, Size = 16)]
public unsafe struct DuckDBStringValue
{
    [FieldOffset(0)]
    public DuckDBStringValuePtr Pointer;
    [FieldOffset(0)]
    public DuckDBStringValueInlined Inlined;

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public string GetValue()
    {
        if (Inlined.Length <= 12)
        {
            fixed (sbyte* pointerToFirst = Inlined.Inlined)
            {
                return new string(pointerToFirst, 0, (int)Inlined.Length);
            }
        }
        else
        {
            var span = new ReadOnlySpan<byte>(Pointer.Ptr, (int)Pointer.Length);
            return Encoding.UTF8.GetString(span);
        }
    }
}
