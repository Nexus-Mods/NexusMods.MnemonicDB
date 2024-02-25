using System;
using System.Collections.Concurrent;
using System.Linq;
using K4os.Compression.LZ4;
using K4os.Compression.LZ4.Encoders;
using NexusMods.EventSourcing.Storage.ValueTypes;

namespace NexusMods.EventSourcing.Storage;

/// <summary>
/// In-memory key-value store.
/// </summary>
public class InMemoryKvStore : IKvStore
{
    private readonly ConcurrentDictionary<StoreKey, (int, Memory<byte>)> _store = new();

    public int Size => _store.Values.Sum(v => v.Item2.Length);

    public void Put(StoreKey key, ReadOnlySpan<byte> value)
    {
        _store[key] = (value.Length, value.ToArray());
    }

    public bool TryGet(StoreKey key, out ReadOnlySpan<byte> value)
    {
        if (_store.TryGetValue(key, out var memory))
        {
            value = memory.Item2.Span;
            return true;
        }

        value = default;
        return false;
    }

    public void Delete(StoreKey key)
    {
        _store.TryRemove(key, out _);
    }
}
