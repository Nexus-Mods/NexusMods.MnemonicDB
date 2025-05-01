using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Reloaded.Memory.Extensions;

namespace NexusMods.MnemonicDB.Abstractions.IndexSegments;

public readonly struct PinnedIndexSegmentBuilder : IIndexSegmentBuilder
{
    private readonly List<(Ptr, Ptr)> _pointers = [];

    public PinnedIndexSegmentBuilder()
    {
        _pointers = [];
    }
    
    /// <summary>
    /// Adds the current item pointed to by the enumerator
    /// </summary>
    public void AddCurrent<T>(in T enumerator) where T : IRefDatomEnumerator, allows ref struct
    {
        _pointers.Add((enumerator.Current, enumerator.ExtraValueSpan));
    }

    /// <summary>
    /// Adds all the items from the enumerator to the segment
    /// </summary>
    public void AddRange<TEnumerator, TDescriptor>(TEnumerator enumerator, TDescriptor descriptor) 
        where TEnumerator : IRefDatomEnumerator
        where TDescriptor : ISliceDescriptor, allows ref struct
    {
        while (enumerator.MoveNext(descriptor)) 
            AddCurrent(enumerator);
    }
    
        /// <summary>
    /// Build the index segment with the given columns
    /// </summary>
    /// <param name="columns"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public Memory<byte> Build(params ReadOnlySpan<IColumn> columns)
    {
        if (_pointers.Count == 0)
            return Memory<byte>.Empty;

        var rowCount = _pointers.Count;
        
        Span<int> columnOffsets = stackalloc int[columns.Length];
        Span<int> columnFixedSizes = stackalloc int[columns.Length];
        
        using var writer = new PooledMemoryBufferWriter();
        
        // Number of rows
        writer.Write(rowCount);
        
        // Column offsets are next
        for (var i = 0 ; i < columns.Length; i++)
        {
            columnOffsets[i] = writer.Length;
            // Columns for each part
            var fixedSize = columns[i].FixedSize;
            columnFixedSizes[i] = fixedSize;
            var partSpan = writer.GetSpan(fixedSize * rowCount);
            writer.Advance(partSpan.Length);
        }
        
        for (var columnIdx = 0; columnIdx < columns.Length; columnIdx++)
        {
            for (var idx = 0; idx < rowCount; idx++)
            {
                var (keyPtr, valuePtr) = _pointers[idx];
                var fixedSize = columnFixedSizes[columnIdx];
                // We have to re-get the span because the writer may have been advanced causing the writer to have to 
                // expand its buffer
                var destWrittenSpan = writer.GetWrittenSpanWritable();
                var destSpan = destWrittenSpan.SliceFast(columnOffsets[columnIdx] + (fixedSize * idx), fixedSize);
                columns[columnIdx].Extract(keyPtr.Span, valuePtr.Span, destSpan, writer);
            }
        }
        return writer.WrittenMemory.ToArray();
    }
    
}
