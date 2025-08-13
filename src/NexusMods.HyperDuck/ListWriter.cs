using System;
using System.Diagnostics;
using Reloaded.Memory.Extensions;

namespace NexusMods.HyperDuck;

public unsafe ref struct ListWriter<TVector, TItem> : IDisposable
    where TVector : IWritableVector, allows ref struct
    where TItem : unmanaged
{
    private ulong _startOffset;
    private ulong _capacity;
    private ulong _currentOffset;
    private Span<TItem> _dataSpan;
    private readonly void* _ptr;
    private readonly void* _subVector;
    private readonly Span<ListEntry> _entrySpan;
    private void* _dataPtr;
    private int _rowIdx;

    public ListWriter(TVector itemVector)
    {
        _ptr = itemVector.GetPtr();
        _startOffset = 0;
        _currentOffset = 0;
        _capacity = ReadOnlyVector.Native.duckdb_list_vector_get_size(_ptr);
        _subVector = ReadOnlyVector.Native.duckdb_list_vector_get_child(_ptr);
        _entrySpan = itemVector.GetData<ListEntry>();
        _dataPtr = ReadOnlyVector.Native.duckdb_vector_get_data(_subVector);
        _dataSpan = new Span<TItem>(_dataPtr, (int)_capacity);
        _rowIdx = 0;
    }
    
    /// <summary>
    /// Get the list entry for the current set of items being written 
    /// </summary>
    public ListEntry GetEntry()
    {
        var len = _currentOffset - _startOffset;
        if (len == 0)
            return default;
        
        return new ListEntry()
        {
            Offset = _startOffset,
            Length = len
        };
    }

    public unsafe void Write(ReadOnlySpan<byte> data)
    {
        if (_currentOffset >= _capacity)
            Expand();
        Debug.Assert(data.Length == sizeof(TItem));
        data.CopyTo(_dataSpan.CastFast<TItem, byte>().SliceFast(sizeof(TItem) * (int)_currentOffset, sizeof(TItem)));
        _currentOffset++;
    }

    private void Expand()
    {
        var newCapacity = Math.Max(_capacity * 2, GlobalConstants.DefaultVectorSize);
        WritableVector.Native.duckdb_list_vector_reserve(_ptr, newCapacity);
        _capacity = newCapacity;
        _dataPtr = ReadOnlyVector.Native.duckdb_vector_get_data(_subVector);
        _dataSpan = new Span<TItem>(_dataPtr, (int)_capacity);
    }

    /// <summary>
    /// Marks the start of the list to the current write position
    /// </summary>
    public void SetStart()
    {
        _startOffset = _currentOffset;
    }

    public void WriteUtf8(ReadOnlySpan<byte> datomsValueSpan)
    {
        if (_currentOffset >= _capacity)
            Expand();
        Debug.Assert(typeof(TItem) == typeof(StringElement));
        fixed (void* ptr = datomsValueSpan)
        {
            WritableVector.Native.duckdb_vector_assign_string_element_len(_subVector, _currentOffset, ptr, (ulong)datomsValueSpan.Length);
        }
        _currentOffset++;
    }

    public void WriteCurrentEntry()
    {
        _entrySpan[_rowIdx] = GetEntry();
        _rowIdx++;
        SetStart();
    }

    public void Dispose()
    {
        WritableVector.Native.duckdb_list_vector_set_size(_ptr, (ulong)_rowIdx);
    }
}
