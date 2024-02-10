using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using NexusMods.EventSourcing.Abstractions;
using Reloaded.Memory.Extensions;
using RocksDbSharp;

namespace NexusMods.EventSourcing.DatomStore;

public class RocksDBDatomStore
{
    private readonly ILogger<RocksDBDatomStore> _logger;
    private readonly DatomStoreSettings _settings;
    private readonly DbOptions _options;
    private readonly RocksDb _db;
    private readonly PooledMemoryBufferWriter _pooledWriter;
    private readonly AttributeRegistry _registry;
    private readonly AIndexDefinition[] _indexes;

    public RocksDBDatomStore(ILogger<RocksDBDatomStore> logger, AttributeRegistry registry, DatomStoreSettings settings)
    {
        _settings = settings;
        _logger = logger;

        _registry = registry;
        _registry.Populate(BuiltInAttributes.Initial);

        _options = new DbOptions()
            .SetCreateIfMissing()
            .SetCompression(Compression.Zstd);

        _db = RocksDb.Open(_options, _settings.Path.ToString(), new ColumnFamilies());

        _indexes =
        [
            new TxIndex(_registry),
            new EATVIndex(_registry),
        ];

        foreach (var index in _indexes)
        {
            index.Init(_db);
        }

        _pooledWriter = new PooledMemoryBufferWriter(128);
    }


    private void Serialize<TVal, TAttr>(WriteBatch batch, ulong e, TVal val, ulong tx, bool isAssert = true)
        where TAttr : IAttribute<TVal>

    {
        _pooledWriter.Reset();
        var header = _pooledWriter.GetSpan(KeyHeader.Size);
        ref var keyHeader = ref MemoryMarshal.AsRef<KeyHeader>(header);
        keyHeader.Entity = e;
        keyHeader.AttributeId = _registry.GetAttributeId<TAttr>();
        keyHeader.IsAssert = isAssert;
        keyHeader.Tx = tx;
        _pooledWriter.Advance(KeyHeader.Size);
        _registry.WriteValue(val, in _pooledWriter);

        var span = _pooledWriter.GetWrittenSpan();
        foreach (var index in _indexes)
        {
            index.Put(batch, span);
        }
    }
}
