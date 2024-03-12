using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace NexusMods.EventSourcing.Storage.Columns.BlobColumns;

[StructLayout(LayoutKind.Explicit, Size = SelfHeaderSize)]
public unsafe struct LowLevelHeader
{
    [FieldOffset(0)]
    private fixed byte _self[1];

    [FieldOffset(0)]
    public uint Count;

    [FieldOffset(4)]
    public uint LengthsOffset;

    [FieldOffset(8)]
    public uint DataOffset;


    public const int SelfHeaderSize = 12;


}
