using System.Buffers;
using System.Runtime.InteropServices;
using NexusMods.EventSourcing.Storage.Columns.ULongColumns;

namespace NexusMods.EventSourcing.Storage.Columns.BlobColumns;

public interface ICanPack : IUnpacked
{

    public IReadable Pack()
    {
        unsafe
        {
            var writer = new PooledMemoryBufferWriter();
            writer.Advance(sizeof(LowLevelHeader));
            Offsets.Pack(writer);
            var lengthsOffset = writer.Length;
            Lengths.Pack(writer);
            var dataOffset = writer.Length;
            writer.Write(Span);

            var casted = MemoryMarshal.Cast<byte, LowLevelHeader>(writer.WrittenSpanWritable);
            casted[0].LengthsOffset = (uint)lengthsOffset;
            casted[0].DataOffset = (uint)dataOffset;

            return null!;
        }
    }

}
