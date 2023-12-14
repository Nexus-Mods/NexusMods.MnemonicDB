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

    public async ValueTask Add<T>(T eventEntity) where T : IEvent
    {
        using var session = _kvStore.NewSession(_functions);
        WriteInner(eventEntity, session);
    }

    private void WriteInner<T>(T eventEntity, ClientSession<byte[], byte[], byte[], byte[], Empty, IFunctions<byte[], byte[], byte[], byte[], Empty>> session) where T : IEvent
    {
        _tx = _tx.Next();
        var ingester = new ModifiedEntityLogger();
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

            var eventBytes = _serializer.Serialize(eventEntity);
            var eventKey = new byte[8];
            BinaryPrimitives.WriteUInt64BigEndian(eventKey, _tx.Value);
            session.RMW(ref eventKey, ref eventBytes);
        }
    }


    /// <summary>
    /// Simplistic context that just logs the entities that were modified.
    /// </summary>
    private readonly struct ModifiedEntityLogger() : IEventContext
    {
        public readonly HashSet<EntityId> Entities  = new();
        public void Emit<TOwner, TVal>(EntityId<TOwner> entity, AttributeDefinition<TOwner, TVal> attr, TVal value) where TOwner : IEntity
        {
            Entities.Add(entity.Value);
        }

        public void Emit<TOwner, TVal>(EntityId<TOwner> entity, MultiEntityAttributeDefinition<TOwner, TVal> attr, EntityId<TVal> value) where TOwner : IEntity where TVal : IEntity
        {
            Entities.Add(entity.Value);
        }

        public void Retract<TOwner, TVal>(EntityId<TOwner> entity, AttributeDefinition<TOwner, TVal> attr, TVal value) where TOwner : IEntity
        {
            Entities.Add(entity.Value);
        }

        public void Retract<TOwner, TVal>(EntityId<TOwner> entity, MultiEntityAttributeDefinition<TOwner, TVal> attr, EntityId<TVal> value) where TOwner : IEntity where TVal : IEntity
        {
            Entities.Add(entity.Value);
        }

        public void New<TType>(EntityId<TType> id) where TType : IEntity
        {
            Entities.Add(id.Value);
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


        for (var idx = 0; idx < (value.Length / 8); idx += 8)
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
