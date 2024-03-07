using System;

namespace NexusMods.EventSourcing.Abstractions;

/// <summary>
/// A blob column that can be appended to.
/// </summary>
public interface IAppendableBlobColumn : IBlobColumn
{
    /// <summary>
    /// Adds the given span to the end of the column.
    /// </summary>
    public void Append(ReadOnlySpan<byte> value);

}
