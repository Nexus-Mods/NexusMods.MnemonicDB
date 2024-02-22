using System.Runtime.InteropServices;

namespace NexusMods.EventSourcing.Storage.Nodes;

[StructLayout(LayoutKind.Explicit)]
public unsafe struct BlockHeader
{
    /// <summary>
    /// The version of the block.
    /// </summary>
    [FieldOffset(0)] public ushort _version;


    [FieldOffset(0)] public ushort _flags;

    /// <summary>
    /// The size of the block, in datoms
    /// </summary>
    [FieldOffset(4)] public uint _datomCount;

    /// <summary>
    /// The size of the blob portion of the block, in bytes
    /// </summary>
    [FieldOffset(8)] public uint _blobSize;
}
