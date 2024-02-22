using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using Cathei.LinqGen;

namespace NexusMods.EventSourcing.Storage.Nodes;

public class AppendableBlock : IStructEnumerable<AppendableBlock.FlyweightDatom, AppendableBlock.FlyweightDatomEnumerator>
{
    private readonly PooledMemoryBufferWriter _pooledMemoryBufferWriter = new();
    private readonly List<ulong> _entityIds = new();
    private readonly List<ushort> _attributeIds = new();
    private readonly List<ulong> _txIds = new();
    private readonly List<byte> _flags = new();
    private readonly List<ulong> _values = new();
    public int Count => _entityIds.Count;

    public void Append<TRawDatom>(in TRawDatom datom)
    where TRawDatom : IRawDatom
    {
        _entityIds.Add(datom.EntityId);
        _attributeIds.Add(datom.AttributeId);
        _txIds.Add(datom.TxId);
        _flags.Add(datom.Flags);
        _values.Add(datom.ValueLiteral);
        if (((DatomFlags)datom.Flags).HasFlag(DatomFlags.InlinedData))
            return;

        var span = datom.ValueSpan;
        var offset = _pooledMemoryBufferWriter.GetWrittenSpan().Length;
        _values.Add((ulong)((offset << 4) | span.Length));
        _pooledMemoryBufferWriter.Write(span);
    }



    public FlyweightDatom this[int index] => new(this, (uint)index);


    public struct FlyweightDatom(AppendableBlock block, uint index) : IRawDatom
    {
        public ulong EntityId => block._entityIds[(int)index];
        public ushort AttributeId => block._attributeIds[(int)index];
        public ulong TxId => block._txIds[(int)index];
        public byte Flags => block._flags[(int)index];
        public ReadOnlySpan<byte> ValueSpan
        {
            get
            {
                if (((DatomFlags)Flags).HasFlag(DatomFlags.InlinedData))
                {
                    return ReadOnlySpan<byte>.Empty;
                }
                var combined = block._values[(int)index];
                var offset = (uint)(combined >> 4);
                var length = (uint)(combined & 0xF);
                return block._pooledMemoryBufferWriter.GetWrittenSpan().Slice((int)offset, (int)length);
            }
        }
        public ulong ValueLiteral => block._values[(int)index];

        public void Expand<TWriter>(out ulong entityId, out ushort attributeId, out ulong txId, out byte flags, in TWriter writer,
            out ulong valueLiteral) where TWriter : IBufferWriter<byte>
        {
            throw new NotImplementedException();
        }
    }

    public struct FlyweightDatomEnumerator(AppendableBlock block, int idx)
        : IEnumerator<FlyweightDatom>
    {
        public void Dispose()
        {
        }

        public bool MoveNext()
        {
            if (idx >= block._entityIds.Count) return false;
            idx++;
            return true;
        }

        public void Reset()
        {
            idx = -1;
        }

        public FlyweightDatom Current => new(block, (uint)idx);

        object IEnumerator.Current => Current;
    }

    public FlyweightDatomEnumerator GetEnumerator()
    {
        return new(this, -1);
    }
}
