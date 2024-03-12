using System.Buffers;
using System.Runtime.InteropServices;
using Reloaded.Memory.Extensions;

namespace NexusMods.EventSourcing.Storage.Columns.BlobColumns;

public interface ICanPack : IUnpacked
{

    public void Pack(PooledMemoryBufferWriter writer)
    {
        unsafe
        {
            var writerOffset = writer.Length;
            writer.Advance(sizeof(LowLevelHeader));
            //Offsets.Pack(writer);
            var lengthsOffset = writer.Length;
            //Lengths.Pack(writer);
            var dataOffset = writer.Length;
            writer.Write(Span);

            var casted = MemoryMarshal.Cast<byte, LowLevelHeader>(writer.WrittenSpanWritable.SliceFast(writerOffset));
            casted[0].Count = (uint)Count;
            casted[0].LengthsOffset = (uint)lengthsOffset;
            casted[0].DataOffset = (uint)dataOffset;
        }
    }


}
