using System;
using System.Buffers.Binary;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using NexusMods.EventSourcing.Abstractions;
using RocksDbSharp;

namespace NexusMods.EventSourcing.Storage.RocksDb;

public class RocksDbKvStore : IKvStore
{
    private readonly ILogger<RocksDbKvStore> _logger;
    private readonly RocksDbSharp.RocksDb _db;

    public RocksDbKvStore(ILogger<RocksDbKvStore> logger, RocksDbKvStoreConfig config)
    {
        _logger = logger;
        var options = new DbOptions()
            .SetCreateIfMissing()
            .SetCompression(Compression.Zstd);

        _db = RocksDbSharp.RocksDb.Open(options, config.Path.ToString());
    }
    public void Put(StoreKey key, ReadOnlySpan<byte> value)
    {
        _logger.LogDebug("Put {Key} {Value}KB", key, value.Length / 1024);
        Span<byte> keySpan = stackalloc byte[sizeof(ulong)];
        BinaryPrimitives.WriteUInt64BigEndian(keySpan, key.Value);
        _db.Put(keySpan, value);
    }

    public bool TryGet(StoreKey key, out ReadOnlySpan<byte> value)
    {
        Span<byte> keySpan = stackalloc byte[sizeof(ulong)];
        BinaryPrimitives.WriteUInt64BigEndian(keySpan, key.Value);
        var data = _db.Get(keySpan);
        if (data is null)
        {
            value = default;
            return false;
        }

        _logger.LogDebug("Get {Key} {Value}KB", key, data.Length / 1024);
        value = data.AsSpan();
        return true;
    }

    public void Delete(StoreKey key)
    {
        throw new NotImplementedException();
    }

    public void Dispose()
    {
        _db.Dispose();
    }
}
