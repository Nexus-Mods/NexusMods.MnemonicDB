using System;

namespace NexusMods.EventSourcing.Abstractions;

public interface ISnapshot : IDisposable
{
    IDatomIterator GetIterator(IndexType type);
}
