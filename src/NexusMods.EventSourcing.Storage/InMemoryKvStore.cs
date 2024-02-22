using System;
using System.Collections.Concurrent;

namespace NexusMods.EventSourcing.Storage;

/// <summary>
/// In-memory key-value store.
/// </summary>
public class InMemoryKvStore : IKvStore
{
    private readonly ConcurrentDictionary<UInt128, Memory<byte>> _store = new();

    public void Put(UInt128 key, ReadOnlySpan<byte> value)
    {
        _store[key] = value.ToArray();
    }

    public bool TryGet(UInt128 key, out ReadOnlySpan<byte> value)
    {
        if (_store.TryGetValue(key, out var memory))
        {
            value = memory.Span;
            return true;
        }

        value = default;
        return false;
    }

    public void Delete(UInt128 key)
    {
        _store.TryRemove(key, out _);
    }
}
