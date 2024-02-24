using System.Runtime.InteropServices;
using NexusMods.EventSourcing.Abstractions;

namespace NexusMods.EventSourcing.Storage.Datoms;

[StructLayout(LayoutKind.Explicit, Size = 8 + 2 + 8 + 1 + 8)]
public unsafe struct UnsafeDatom
{
    [FieldOffset(0)] public ulong _entity;
    [FieldOffset(8)] public ushort _attribute;
    [FieldOffset(10)] public ulong _tx;
    [FieldOffset(18)] public DatomFlags _flags;
    [FieldOffset(19)] public ulong _valueLiteral;
}
