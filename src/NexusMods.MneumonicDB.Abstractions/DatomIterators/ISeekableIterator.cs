using System;
using Microsoft.Win32;
using NexusMods.MneumonicDB.Abstractions.Internals;

namespace NexusMods.MneumonicDB.Abstractions.DatomIterators;

/// <summary>
///     An iterator that can seek to the end, start or beginning.
/// </summary>
public interface ISeekableIterator
{
    /// <summary>
    ///     Move to the last element, returning this, casted to an
    ///     IIterator
    /// </summary>
    public IIterator SeekLast();

    /// <summary>
    ///     Seek to the first datom before the given datom, returns this iterator
    ///     casted to an IIterator
    /// </summary>
    public IIterator Seek(ReadOnlySpan<byte> datom);

    /// <summary>
    ///     Set the iterator to the start of the datoms, returning this, casted
    ///     to an IIterator
    /// </summary>
    public IIterator SeekStart();

    /// <summary>
    /// The registry for the attributes.
    /// </summary>
    public IAttributeRegistry Registry { get; }
}
