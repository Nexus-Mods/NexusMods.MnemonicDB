using System;
using System.Buffers;
using System.Buffers.Binary;

namespace NexusMods.DatomStore;

public class BlockWriter
{
    private int _datomCount;
    private readonly IMemoryOwner<byte> _eVals;
    private readonly IMemoryOwner<byte> _aVals;
    private readonly IMemoryOwner<byte> _vVals;
    private readonly IMemoryOwner<byte> _txVals;
    private readonly IMemoryOwner<byte> _opVals;
    private readonly IMemoryOwner<byte> _blobs;
    private readonly int _blobUsed;

    public BlockWriter(int maxDatoms = ushort.MaxValue)
    {
        _datomCount = 0;
        _eVals = MemoryPool<byte>.Shared.Rent(maxDatoms * 8);
        _aVals = MemoryPool<byte>.Shared.Rent(maxDatoms * 8);
        _vVals = MemoryPool<byte>.Shared.Rent(maxDatoms * 9);
        _txVals = MemoryPool<byte>.Shared.Rent(maxDatoms * 8);
        _opVals = MemoryPool<byte>.Shared.Rent(maxDatoms / 8);
        _blobs = MemoryPool<byte>.Shared.Rent(1024 * 1024); // 1MB
        _blobUsed = 0;
    }

    public int DatomCount => _datomCount;

    public void Write(ulong e, ulong a, ulong v, ulong t, Op op = Op.Assert)
    {
        BinaryPrimitives.WriteUInt64BigEndian(_eVals.Memory.Span.Slice(_datomCount * 8), e);
        BinaryPrimitives.WriteUInt64BigEndian(_aVals.Memory.Span.Slice(_datomCount * 8), a);
        _vVals.Memory.Span.Slice(_datomCount * 9, 1)[0] = (byte)ValueTypes.Ulong;
        BinaryPrimitives.WriteUInt64BigEndian(_vVals.Memory.Span.Slice(_datomCount * 9 + 1), v);
        BinaryPrimitives.WriteUInt64BigEndian(_txVals.Memory.Span.Slice(_datomCount * 8), t);
        if (op == Op.Assert)
        {
            _opVals.Memory.Span[_datomCount / 8] |= (byte)(1 << (7 - (_datomCount % 8)));
        }
        else
        {
            _opVals.Memory.Span[_datomCount / 8] &= (byte)~(1 << (7 - (_datomCount % 8)));
        }
        _datomCount++;
    }

    /// <summary>
    /// Calculates the total storage size of the block
    /// </summary>
    public long BlockSize => 3 + _datomCount * (8 + 8 + 9 + 8 + 1) + _blobUsed;

    public void WriteTo(Span<byte> blob)
    {
        blob[0] = 1; // version
        BinaryPrimitives.WriteUInt16BigEndian(blob.Slice(1, 2), (ushort)_datomCount);
        _eVals.Memory.Span.Slice(0, _datomCount * 8).CopyTo(blob.Slice(3));
        _aVals.Memory.Span.Slice(0, _datomCount * 8).CopyTo(blob.Slice(3 + _datomCount * 8));
        _vVals.Memory.Span.Slice(0, _datomCount * 9).CopyTo(blob.Slice(3 + _datomCount * 16));
        _txVals.Memory.Span.Slice(0, _datomCount * 8).CopyTo(blob.Slice(3 + _datomCount * 25));
        _opVals.Memory.Span.Slice(0, _datomCount / 8).CopyTo(blob.Slice(3 + _datomCount * 33));
        _blobs.Memory.Span.Slice(0, _blobUsed).CopyTo(blob.Slice(3 + _datomCount * 33 + _datomCount / 8));
    }
}
