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
}

public interface IReadable<T> : IReadable where T : struct
{
    public T this[int idx] { get; }

    public void CopyTo(int offset, Span<T> dest);

    /// <summary>
    /// Expands this column into an appendable column with the same type. Think
    /// of this as the opposite of the "pack" operation.
    /// </summary>
    /// <returns></returns>
    public Appendable<T> Unpack()
    {
        var appendable = Appendable<T>.Create(Length);
        CopyTo(0, appendable.GetWritableSpan(Length));
        appendable.SetLength(Length);
        return appendable;
    }
}
