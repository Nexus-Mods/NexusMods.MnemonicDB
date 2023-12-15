using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using FASTER.core;
using NexusMods.EventSourcing.Abstractions;
using Reloaded.Memory;

namespace NexusMods.EventSourcing.FasterKV;

public class FasterKVEventStore<TSerializer> : IEventStore
    where TSerializer : IEventSerializer
{
    private readonly FasterKVSettings<byte[], byte[]> _settings;
    private readonly FasterKV<byte[], byte[]> _kvStore;
    private TransactionId _tx;
    private readonly TSerializer _serializer;
    private readonly SimpleFunctions<byte[], byte[]> _functions;
    private readonly Task _timer;


    public FasterKVEventStore(TSerializer serializer, Settings settings)
    {
        _serializer = serializer;
        _settings = new FasterKVSettings<byte[], byte[]>(settings.StorageLocation.ToString());
        _kvStore = new FasterKV<byte[], byte[]>(_settings);
        _tx = TransactionId.From(0);

        _functions = new SimpleFunctions<byte[], byte[]>((input, oldValue) =>
        {
            var newMemory = new byte[oldValue.Length + input.Length];
            Array.Copy(oldValue, newMemory, oldValue.Length);
            Array.Copy(input, 0, newMemory, oldValue.Length, input.Length);
            return newMemory;
        });

        _timer = Task.Run(async () =>
        {
            await CheckPoint();
            await Task.Delay(TimeSpan.FromSeconds(10));
        });

    }

    public async Task CheckPoint()
    {
        var sw = Stopwatch.StartNew();
        await _kvStore.TakeFullCheckpointAsync(CheckpointType.Snapshot);
        Console.WriteLine($"Checkpoint took {sw.ElapsedMilliseconds}ms");
    }

    public async ValueTask Flush()
    {
        await _kvStore.CompleteCheckpointAsync();
    }

    public TransactionId Add<T>(T eventEntity) where T : IEvent
    {
        lock (this)
        {
            _tx = _tx.Next();
            using var session = _kvStore.NewSession(_functions);
            WriteInner(eventEntity, session);
            return _tx;
        }
    }

    private void WriteInner<T>(T eventEntity, ClientSession<byte[], byte[], byte[], byte[], Empty, IFunctions<byte[], byte[], byte[], byte[], Empty>> session) where T : IEvent
    {
        var ingester = new ModifiedEntitiesIngester();
        eventEntity.Apply(ingester);
        {
            var key = new byte[16];
            var value = new byte[8];
            BinaryPrimitives.WriteUInt64BigEndian(value, _tx.Value);

            foreach (var entityId in ingester.Entities)
            {
                entityId.Value.TryWriteBytes(key);
                session.RMW(ref key, ref value);
            }

            Console.WriteLine($"Wrote event to {ingester.Entities.Count} entities {eventEntity.ToString()}, {_tx.Value}");
            var eventBytes = _serializer.Serialize(eventEntity).ToArray();
            var eventKey = new byte[8];
            BinaryPrimitives.WriteUInt64BigEndian(eventKey, _tx.Value);
            session.RMW(ref eventKey, ref eventBytes);
        }
    }




    public void EventsForEntity<TIngester>(EntityId entityId, TIngester ingester) where TIngester : IEventIngester
    {
        var key = new byte[16];
        entityId.Value.TryWriteBytes(key);
        using var session = _kvStore.NewSession(_functions);
        var value = Array.Empty<byte>();
        var result = session.Read(ref key, ref value);
        Debug.Assert(result.Found);


        for (var idx = 0; idx < value.Length; idx += 8)
        {
            var span = value.AsSpan(idx, 8).ToArray();
            var eventArray = Array.Empty<byte>();
            var eventResult = session.Read(ref span, ref eventArray);
            Debug.Assert(eventResult.Found);

            var evt = _serializer.Deserialize(eventArray);
            ingester.Ingest(evt);
        }

    }
}
