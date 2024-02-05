using System.Runtime.InteropServices;

namespace NexusMods.EventSourcing;

/// <summary>
/// Some DuckDB blobs (and strings are blobs) are inlined, some are pointers. This struct is a union of both
/// and provides a way to access the data in a consistent way, but also in a way that matches the memory layout
/// of data in the C library.
/// </summary>
public partial struct DuckDBBlob
{
    public _value_e__Union value;

    private const int InlineStringMaxLength = 12;

    public readonly int Length => (int)value.inlined.length;

    public readonly unsafe sbyte* Data
    {
        get
        {
            if (Length <= InlineStringMaxLength)
            {
                fixed (sbyte* pointerToFirst = value.inlined.inlined)
                {
                    return pointerToFirst;
                }
            }
            else
            {
                return value.pointer.ptr;
            }
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    public partial struct _value_e__Union
    {
        [FieldOffset(0)]
        public DuckDBStringPointer pointer;

        [FieldOffset(0)]
        public DuckDBStringInlined inlined;

        public unsafe partial struct DuckDBStringPointer
        {
            public uint length;

            public fixed sbyte prefix[4];

            public sbyte* ptr;
        }

        public unsafe partial struct DuckDBStringInlined
        {
            public uint length;

            public fixed sbyte inlined[12];
        }
    }
}
