using System;

namespace NexusMods.MneumonicDB.Abstractions.DatomIterators;

/// <summary>
///     The base interface for a datom iterator, also implements
///     IDisposable to allow for cleanup
/// </summary>
public interface IDatomSource : ISeekableIterator, IDisposable { }
