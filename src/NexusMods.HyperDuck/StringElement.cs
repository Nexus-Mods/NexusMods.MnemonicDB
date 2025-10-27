using System;
using System.Runtime.InteropServices;
using System.Text;
using CommunityToolkit.HighPerformance.Buffers;

namespace NexusMods.HyperDuck;

[StructLayout(LayoutKind.Explicit, Size = ElementSize)]
public unsafe struct StringElement
{
    public const int InlineSize = 12;
    public const int ElementSize = 16;
    [FieldOffset(0)]
    public StringPointer Pointer;
    
    [FieldOffset(0)]
    public InlineString Inline;
    
    public bool IsInline => Inline.Length <= InlineSize;
    public bool IsPointer => !IsInline;
    
    public string GetString()
    {
        if (IsInline)
        {
            fixed (byte* data = Inline.Data)
            {
                return Encoding.UTF8.GetString(data, (int)Inline.Length);
            }
        }
        else
        {
            return Encoding.UTF8.GetString(Pointer.Ptr, (int)Pointer.Length);
        }
    }
    
    public string GetString(StringPool pool)
    {
        if (IsInline)
        {
            fixed (byte* data = Inline.Data)
            {
                var span = new ReadOnlySpan<byte>(data, (int)Inline.Length);
                return pool.GetOrAdd(span, Encoding.UTF8);
            }
        }
        else
        {
            return pool.GetOrAdd(new ReadOnlySpan<byte>(Pointer.Ptr, (int)Pointer.Length), Encoding.UTF8);
        }
    }

    public readonly ReadOnlySpan<byte> GetSpan()
    {
        if (IsInline)
        {
            fixed (byte* data = Inline.Data)
            {
                return new ReadOnlySpan<byte>(data, (int)Inline.Length);
            }
        }
        else
        {
            return new ReadOnlySpan<byte>(Pointer.Ptr, (int)Pointer.Length);
        }
    }
    
    public static implicit operator string(StringElement element)
    {
        return element.GetString();
    }

    public unsafe struct StringPointer
    {
        internal uint Length;
        fixed byte Prefix[4];
        internal byte* Ptr;
    }
    
    public unsafe struct InlineString
    {
        public uint Length;
        public fixed byte Data[InlineSize];
    }
    
}
