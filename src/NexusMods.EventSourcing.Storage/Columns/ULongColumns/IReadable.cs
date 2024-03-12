using System;

namespace NexusMods.EventSourcing.Storage.Columns.ULongColumns;

/// <summary>
/// A readable column of ulong values.
/// </summary>
public interface IReadable
{
    /// <summary>
    /// Gets the length of the column in rows.
    /// </summary>
    public int Length { get; }

    /// <summary>
    /// Copies the column to the specified destination offset by the specified offset.
    /// </summary>
    public void CopyTo(int offset, Span<ulong> dest);


    /// <summary>
    /// Get the value at the given index.
    /// </summary>
    public ulong this[int idx] { get; }

    public Appendable Unpack()
    {
        var appendable = Appendable.Create(Length);
        CopyTo(0, appendable.GetWritableSpan(Length));
        appendable.SetLength(Length);
        return appendable;
    }
}
