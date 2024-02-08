using System.Buffers;
using System.Buffers.Binary;
using FsCheck.FSharp;
using FsCheck.Xunit;

namespace NexusMods.DatomStore.Tests;

public class BlockTests
{

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(4)]
    [InlineData(8)]
    [InlineData(16)]
    [InlineData(32)]
    [InlineData(64)]
    [InlineData(128)]
    [InlineData(256)]
    [InlineData(512)]
    [InlineData(1024)]
    [InlineData(2048)]
    [InlineData(4096)]
    [InlineData(8192)]
    [InlineData(16384)]
    [InlineData(32768)]
    [InlineData(65535)]
    public void CanCreateSimpleBlock(int count)
    {

        var data = TestData.UlongData(count);

        var writer = new BlockWriter();

        for (var i = 0; i < data.Length; i++)
        {
            writer.Write(data[i].Item1, data[i].Item2, data[i].Item3, 0);
        }

        writer.DatomCount.Should().Be(data.Length);
        writer.BlockSize.Should().Be(3 + data.Length * (8 + 8 + 9 + 8 + 1));

        using var block = MemoryPool<byte>.Shared.Rent((int)writer.BlockSize);
        writer.WriteTo(block.Memory.Span);

        var readBlock = new InMemoryBlock(block.Memory);

        readBlock.Version.Should().Be(1);
        readBlock.DatomCount.Should().Be((ushort)data.Length);

        var datom = readBlock.Iterator();
        for (int idx = 0; idx < data.Length; idx++)
        {
            datom.SeekTo(idx);
            datom.Entity.Should().Be(data[idx].Item1);
            datom.Attribute.Should().Be(data[idx].Item2);
            datom.ValueType.Should().Be(ValueTypes.Ulong);
            datom.ValueSpan.Length.Should().Be(8);
            BinaryPrimitives.ReadUInt64BigEndian(datom.ValueSpan).Should().Be(data[idx].Item3, "value at index {0} should match", idx);
        }

    }

}
