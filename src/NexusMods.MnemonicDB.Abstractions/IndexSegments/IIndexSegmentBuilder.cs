using System;
using NexusMods.MnemonicDB.Abstractions.IndexSegments.Columns;

namespace NexusMods.MnemonicDB.Abstractions.IndexSegments;

/// <summary>
/// A interface for a segment builder
/// </summary>
public interface IIndexSegmentBuilder
{
    /// <summary>
    /// Build extract the given columns and create a memory block of the columns
    /// </summary>
    public Memory<byte> Build(params ReadOnlySpan<IColumn> columns);
    
    
    /// <summary>
    /// Build a segment with the given columns
    /// </summary>
    public Memory<byte> Build<TValue1>() 
    {
        return Build(ColumnDefinitions.ColumnFor<TValue1>());
    }
    
    /// <summary>
    /// Build a segment with the given columns
    /// </summary>
    public Memory<byte> Build<TValue1, TValue2>() 
    {
        return Build(ColumnDefinitions.ColumnFor<TValue1>(), ColumnDefinitions.ColumnFor<TValue2>());
    }

    /// <summary>
    /// Build a segment with the given columns
    /// </summary>
    public Memory<byte> Build<TValue1, TValue2, TValue3>() 
    {
        return Build(ColumnDefinitions.ColumnFor<TValue1>(), ColumnDefinitions.ColumnFor<TValue2>(), ColumnDefinitions.ColumnFor<TValue3>());
    }

}
