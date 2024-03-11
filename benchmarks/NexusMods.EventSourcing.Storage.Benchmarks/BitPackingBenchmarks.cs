using System;
using System.Numerics;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using BenchmarkDotNet.Attributes;

namespace NexusMods.EventSourcing.Storage.Benchmarks;

public class BitPackingBenchmarks
{
    private readonly ulong[] _unpackedData;
    private ushort[] _packedShorts = null!;
    private byte[] _bitPackedData = null!;

    public BitPackingBenchmarks()
    {
        _unpackedData = new ulong[1024];
        for (var i = 0; i < _unpackedData.Length; i++)
        {
            _unpackedData[i] = (ulong)i;
        }

        PackShort();
        PackBits();
    }

    private void PackShort()
    {
        _packedShorts = new ushort[1024];
        for (var i = 0; i < _unpackedData.Length; i++)
        {
            _packedShorts[i] = (ushort)(_unpackedData[i] & 0xFFFF);
        }
    }

    private void PackBits()
    {
        _bitPackedData = new byte[10 * 1024 / 8 + 8];


        void SetBits(int bitOffset, ushort value)
        {
            var byteOffset = bitOffset / 8;
            var bitShift = bitOffset % 8;
            _bitPackedData[byteOffset] |= (byte)(value << bitShift);
            _bitPackedData[byteOffset + 1] |= (byte)(value >> (8 - bitShift));
        }

        for (var i = 0; i < _unpackedData.Length; i++)
        {
            SetBits(i * 10, _packedShorts[i]);
        }
    }

    [Benchmark]
    public ulong UnpackShorts()
    {
        ulong sum = 0;
        var span = _packedShorts.AsSpan();
        for (var i = 0; i < _packedShorts.Length; i++)
        {
            sum = span[i];
        }
        return sum;
    }

    [Benchmark]
    public ulong UnpackBits()
    {
        ulong sum = 0;
        var span = _bitPackedData.AsSpan();
        for (var i = 0; i < _unpackedData.Length; i++)
        {
            var byteOffset = i * 10 / 8;
            var bitShift = i * 10 % 8;
            sum += (ulong)(span[byteOffset] >> bitShift | span[byteOffset + 1] << (8 - bitShift));
        }

        return sum;
    }

}
