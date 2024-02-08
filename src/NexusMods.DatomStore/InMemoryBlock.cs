using System;
using System.Collections;
using System.Diagnostics;
using static System.Buffers.Binary.BinaryPrimitives;

namespace NexusMods.DatomStore;

public class InMemoryBlock : IBlock
{
    private readonly Memory<byte> _data;
    private readonly ushort _datomsCount;
    private readonly ulong _entityIdOffset;
    private readonly ulong _attributeOffset;
    private readonly ulong _valueOffsets;
    private readonly ulong _txOffset;
    private readonly ulong _opOffset;
    private readonly ulong _dataOffset;


    public InMemoryBlock(Memory<byte> data)
    {
        _data = data;
        _datomsCount = ReadUInt16BigEndian(_data.Span.Slice(1, 2));
        _entityIdOffset = 3;
        _attributeOffset = _entityIdOffset + ((ulong)_datomsCount * 8);
        _valueOffsets = _attributeOffset + ((ulong)_datomsCount * 8);
        _txOffset = _valueOffsets + ((ulong)_datomsCount * 9);
        _opOffset = _txOffset + ((ulong)_datomsCount * 8);
        _dataOffset = _opOffset + ((ulong)_datomsCount / 8);
    }

    public byte Version => _data.Span[0];
    public ushort DatomCount => ReadUInt16BigEndian(_data.Span.Slice(1, 2));

    public SeekableIterator Iterator()
    {
        return new SeekableIterator(this);
    }


    public class SeekableIterator : ADatom
    {
        private int _offset = -1;
        private readonly InMemoryBlock _block;

        public SeekableIterator(InMemoryBlock block)
        {
            _block = block;
        }

        public void SeekTo(int idx)
        {
            Debug.Assert(idx >= 0 && idx < _block._datomsCount);
            _offset = idx;
        }

        public override ulong Entity => ReadUInt64BigEndian(_block._data.Span.Slice((int)(_block._entityIdOffset + ((ulong)_offset * 8)), 8));
        public override ulong Attribute => ReadUInt64BigEndian(_block._data.Span.Slice((int)(_block._attributeOffset + ((ulong)_offset * 8)), 8));
        public override ValueTypes ValueType => (ValueTypes)_block._data.Span[(int)(_block._valueOffsets + ((ulong)_offset * 9))];
        public override ReadOnlySpan<byte> ValueSpan
        {
            get
            {
                if (ValueType <= ValueTypes.Reference)
                    return _block._data.Span.Slice((int)(_block._valueOffsets + ((ulong)_offset * 9) + 1), 8);

                var length = ReadUInt32BigEndian(_block._data.Span.Slice((int)(_block._valueOffsets + ((ulong)_offset * 9)), 4));
                var offset = ReadUInt32BigEndian(_block._data.Span.Slice((int)(_block._valueOffsets + ((ulong)_offset * 9)) + 4, 4));
                return _block._data.Span.Slice((int)(_block._dataOffset + offset), (int)length);
            }
        }

        public override ulong Tx => ReadUInt64BigEndian(_block._data.Span.Slice((int)(_block._txOffset + ((ulong)_offset * 8)), 8));
    }
}
