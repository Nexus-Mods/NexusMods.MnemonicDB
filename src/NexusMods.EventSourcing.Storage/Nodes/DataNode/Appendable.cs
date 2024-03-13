using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Storage.Columns.BlobColumns;

namespace NexusMods.EventSourcing.Storage.Nodes.DataNode;

public class Appendable : ADataNode, IAppendableNode
{
    private Columns.ULongColumns.Appendable _entityIds;
    private Columns.ULongColumns.Appendable _attributeIds;
    private Columns.BlobColumns.Appendable _values;
    private Columns.ULongColumns.Appendable _transactionIds;
    private readonly int _length;

    public Appendable(int initialSize = Columns.ULongColumns.Appendable.DefaultSize)
    {
        _length = 0;
        _entityIds = Columns.ULongColumns.Appendable.Create();
        _attributeIds = Columns.ULongColumns.Appendable.Create();
        _values = Columns.BlobColumns.Appendable.Create();
        _transactionIds = Columns.ULongColumns.Appendable.Create();
    }

    public Appendable(IReadable readable)
    {
        _length = readable.Count;
        _entityIds = Columns.ULongColumns.Appendable.Unpack(readable);
        _attributeIds = Columns.ULongColumns.Appendable.Unpack(readable.GetAttributeIdColumn());
        _values = Columns.BlobColumns.Appendable.Unpack(readable.GetValueColumn());
        _transactionIds = Columns.ULongColumns.Appendable.Unpack(readable.GetTransactionIdColumn());
    }

    public override IEnumerator<Datom> GetEnumerator()
    {
        for (var i = 0; i < _length; i++)
        {
            yield return new Datom
            {
                E = EntityId.From(_entityIds[i]),
                A = AttributeId.From(_attributeIds[i]),
                V = _values.GetMemory(i),
                T = TxId.From(_transactionIds[i])
            };
        }
    }

    public override long DeepLength => _length;
    public override int Length => _length;

    public override Datom this[int idx] => new()
    {
        E = EntityId.From(_entityIds[idx]),
        A = AttributeId.From(_attributeIds[idx]),
        V = _values.GetMemory(idx),
        T = TxId.From(_transactionIds[idx])
    };
    public override Datom LastDatom => this[_length - 1];
    public override void WriteTo<TWriter>(TWriter writer)
    {
        throw new NotImplementedException();
    }

    public override IDataNode Flush(INodeStore store)
    {
        throw new NotImplementedException();
    }

    public override EntityId GetEntityId(int idx)
    {
        return EntityId.From(_entityIds[idx]);
    }

    public override AttributeId GetAttributeId(int idx)
    {
        return AttributeId.From(_attributeIds[idx]);
    }

    public override TxId GetTransactionId(int idx)
    {
        return TxId.From(_transactionIds[idx]);
    }

    public override ReadOnlySpan<byte> GetValue(int idx)
    {
        return _values[idx];
    }

    #region Class Specifc Methods

    public void SetTx(TxId nextTx)
    {
        throw new NotImplementedException();
    }

    public void RemapEntities(Func<EntityId, EntityId> remapper, AttributeRegistry registry)
    {
        throw new NotImplementedException();
        /*
        unsafe
        {
            fixed (EntityId* entityIdsPtr = _entityIds.Data)
            {
                for (var i = 0; i < _entityIds.Length; i++)
                {
                    var attrId = _attributeIds[i];
                    if (registry.IsReference(_attributeIds[i]))
                    {
                        _values.RemapEntities(i, remapper);
                    }
                    entityIdsPtr[i] = remapper(entityIdsPtr[i]);
                }
            }
        }
        */
    }


    #endregion

    public void Append<TValue>(EntityId e, AttributeId a, TxId t, DatomFlags f, IValueSerializer<TValue> serializer, TValue value)
    {
        _entityIds.Append(e.Value);
        _attributeIds.Append(a.Value);
        _values.Append(serializer, value);
        _transactionIds.Append(t.Value);
    }

    /// <summary>
    /// Analyze the column and pack it into a more efficient representation, this will either be a constant
    /// value, an unpacked array, or a packed array. Packed arrays use a bit of bit twiddling to efficiently
    /// store the most common patterns of ids in the system
    /// </summary>
    public IReadable Pack()
    {
        var stats = Statistics.Create(MemoryMarshal.Cast<ulong, ulong>(Span));
        return (IReadable)Pack(stats);
    }
    private ULongPackedColumn Pack(Statistics stats)
    {
        switch (stats.GetKind())
        {
            // Only one value appears in the column
            case UL_Column_Union.ItemKind.Constant:
                return new ULongPackedColumn
                {
                    Length = stats.Count,
                    Header = new UL_Column_Union(
                        new UL_Constant
                        {
                            Value = stats.MinValue
                        }),
                    Data = Memory<byte>.Empty,
                };

            // Packing won't help, so just pack it down to a struct
            case UL_Column_Union.ItemKind.Unpacked:
            {
                return new ULongPackedColumn
                {
                    Length = stats.Count,
                    Header = new UL_Column_Union(
                        new UL_Unpacked
                        {
                            Unused = 0
                        }),
                    Data = new Memory<byte>(Span.CastFast<ulong, byte>().SliceFast(0, sizeof(ulong) * stats.Count).ToArray()),
                };
            }

            // Pack the column. This process looks at the partition byte (highest byte) and the remainder of the
            // ulong. It then diffs the highest and lowest values in each section to find the offsets. It then
            // stores the offsets and each value becomes a pair of (value, partition). The pairs always fall on
            // byte boundaries, but the bytes can be odd numbers, anywhere from 1 to 7 bytes per value. We make sure
            // the resulting chunk is large enough that we can over-read and mask values without overrunning the
            // allocated memory.
            case UL_Column_Union.ItemKind.Packed:
            {
                var destData = GC.AllocateUninitializedArray<byte>(stats.TotalBytes * stats.Count + 8);

                var srcSpan = Span.CastFast<ulong, ulong>().SliceFast(0, stats.Count);
                var destSpan = destData.AsSpan();

                const ulong valueMask = 0x00FFFFFFFFFFFFFFUL;

                var valueOffset = stats.MinValue;
                var partitionOffset = stats.MinPartition;

                for (var idx = 0; idx < Span.Length; idx += 1)
                {
                    var srcValue = srcSpan[idx];
                    var partition = (byte)(srcValue >> (8 * 7)) - partitionOffset;
                    var value = (srcValue & valueMask) - valueOffset;

                    var packedValue = value << stats.PartitionBits | (byte)partition;
                    var slice = destSpan.SliceFast(stats.TotalBytes * idx);
                    MemoryMarshal.Write(slice, packedValue);
                }

                return new ULongPackedColumn
                {
                    Length = stats.Count,
                    Header = new UL_Column_Union(
                        new UL_Packed
                        {
                            ValueOffset = valueOffset,
                            PartitionOffset = partitionOffset,
                            ValueBytes = stats.TotalBytes,
                            PartitionBits = stats.PartitionBits
                        }),
                    Data = new Memory<byte>(destData),
                };
            }
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}
