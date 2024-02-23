using System;

namespace NexusMods.EventSourcing.Storage.Nodes;

/*
public unsafe struct OffHeapBlock
{
    private readonly void* _data;
    private readonly uint _size;

    public OffHeapBlock(void* ptr, uint size)
    {
        _data = ptr;
        _size = size;
    }

    private BlockHeader* Header => (BlockHeader*) _data;

    /// <summary>
    /// E = 8 bytes
    /// A = 2 bytes
    /// TX = 8 bytes
    /// Flags = 1 byte
    /// </summary>
    private int DataPerDatom = sizeof(ulong) + sizeof(ulong) + sizeof(ushort) + 1;


    /// <summary>
    /// The full size of the block, in bytes
    /// </summary>
    private int CalculatedSize => sizeof(BlockHeader) + (int)(DataPerDatom * Header->_datomCount) + (int)Header->_blobSize;


    public uint DatomCount => Header->_datomCount;

    public FlyweightRawDatom this[uint index]
    {
        get
        {
            if (index >= Header->_datomCount)
                throw new ArgumentOutOfRangeException(nameof(index));
            var datom = (byte*) _data + sizeof(BlockHeader) + index * DataPerDatom;
            return new FlyweightRawDatom(datom, index);
        }
    }



}
*/
