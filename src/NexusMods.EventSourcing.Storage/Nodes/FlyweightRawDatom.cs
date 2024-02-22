using System;
using System.Buffers;

namespace NexusMods.EventSourcing.Storage.Nodes;

public unsafe struct FlyweightRawDatom : IRawDatom
{
    private readonly byte* _data;
    private readonly uint _idx;

    public FlyweightRawDatom(byte* data, uint idx)
    {
        _data = data;
        _idx = idx;
    }

    private BlockHeader* Header => (BlockHeader*) _data;
    private byte* Data => _data + sizeof(BlockHeader);

    public ulong EntityId => *(ulong*) (Data + _idx * sizeof(ulong));
    public ushort AttributeId => *(ushort*) (Data + _idx * sizeof(ulong) + sizeof(ulong));
    public ulong TxId => *(ulong*) (Data + _idx * sizeof(ulong) + sizeof(ulong) + sizeof(ushort));
    public byte Flags => *(Data + _idx * sizeof(ulong) + sizeof(ulong) + sizeof(ulong) + sizeof(ushort));

    public ReadOnlySpan<byte> ValueSpan
    {
        get
        {
            if (((DatomFlags)Flags).HasFlag(DatomFlags.InlinedData))
            {
                return new ReadOnlySpan<byte>(Data + _idx * sizeof(ulong) + sizeof(ulong) + sizeof(ulong) + sizeof(ushort) + 1, 8);
            }
            else
            {
                var blobOffset = *(uint*) (Data + _idx * sizeof(ulong) + sizeof(ulong) + sizeof(ulong) + sizeof(ushort) + 1);
                var blobSize = *(uint*) (Data + _idx * sizeof(ulong) + sizeof(ulong) + sizeof(ulong) + sizeof(ushort) + 1 + sizeof(uint));
                return new ReadOnlySpan<byte>(Data + blobOffset, (int)blobSize);
            }
        }
    }

    public ulong ValueLiteral { get; }

    public void Expand<TWriter>(out ulong entityId, out ushort attributeId, out ulong txId, out byte flags, in TWriter writer,
        out ulong valueLiteral) where TWriter : IBufferWriter<byte>
    {
        throw new NotImplementedException();
    }
}
