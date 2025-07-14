using System.Runtime.InteropServices;
using System.Text;

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