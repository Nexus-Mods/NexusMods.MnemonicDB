using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace NexusMods.MnemonicDB.ManagedTree;

/// <summary>
/// A block that acts like a IBufferWriter, but allows for the creation of a new
/// "row" of data, which will reset the writer, while keeping the previously written data. 
/// </summary>
public class WritableBlock : IBufferWriter<byte>, IDisposable
{
    private IMemoryOwner<byte> _data;
    private int _dataOffset;
    private List<int> _offsets;

    public WritableBlock() : this(1024)
    {
        
    }
    
    public WritableBlock(int dataSize)
    {
        _dataOffset = 0;
        _data = MemoryPool<byte>.Shared.Rent(dataSize);
        _offsets = [];
    }

    /// <summary>
    /// Number of rows written to the block
    /// </summary>
    public int RowCount => _offsets.Count;

    public void NextRow()
    {
        _offsets.Add(_dataOffset);
    }
    
    public void Advance(int count)
    {
        _dataOffset += count;
    }


    public Memory<byte> GetMemory(int sizeHint = 0)
    {
        var required = _dataOffset + sizeHint;
        EnsureSize(required);
        return _data.Memory.Slice(_dataOffset, sizeHint);
    }

    public Span<byte> GetSpan(int sizeHint = 0)
    {
        if (sizeHint == 0)
        {
            var capacity = _data.Memory.Length - _dataOffset;
            if (capacity == 0) 
                EnsureSize(_data.Memory.Length + 128);

            return _data.Memory.Span.Slice(_dataOffset);
        }
        var required = _dataOffset + sizeHint;
        EnsureSize(required);
        return _data.Memory.Span.Slice(_dataOffset, sizeHint);
    }

    private void EnsureSize(int required)
    {
        if (_data.Memory.Length >= required)
            return;

        var newMemory = MemoryPool<byte>.Shared.Rent(required * 2);
        _data.Memory.CopyTo(newMemory.Memory);
        _data.Dispose();
        _data = newMemory;
    }

    public void Dispose()
    {
        _data.Dispose();
    }

    public ReadOnlySpan<byte> this[int idx]
    {
        get
        {
            var startIdx = idx == 0 ? 0 : _offsets[idx - 1];
            var endIdx = _offsets[idx];
            return _data.Memory.Span.Slice(startIdx, endIdx - startIdx);
        }
    }
}
