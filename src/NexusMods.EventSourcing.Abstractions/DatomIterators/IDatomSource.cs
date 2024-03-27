using System;

namespace NexusMods.EventSourcing.Abstractions.DatomIterators;

/// <summary>
/// The base interface for a datom iterator, also implements
/// IDisposable to allow for cleanup
/// </summary>
public interface IDatomSource : ISeekableIterator, IDisposable
{

}
