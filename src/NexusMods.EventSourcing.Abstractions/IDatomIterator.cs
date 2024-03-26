using System;

namespace NexusMods.EventSourcing.Abstractions;

public interface IDatomIterator : IDisposable
{
    /// <summary>
    /// True if the iterator is valid
    /// </summary>
    public bool Valid { get; }

    /// <summary>
    /// Advance the iterator to the next element
    /// </summary>
    public void Next();

    /// <summary>
    /// Seek to the first datom before the given datom
    /// </summary>
    public void Seek(ReadOnlySpan<byte> datom);

    /// <summary>
    /// The current datom
    /// </summary>
    public ReadOnlySpan<byte> Current { get; }

    /// <summary>
    /// Set the iterator to the start of the datoms
    /// </summary>
    void SeekStart();
}
