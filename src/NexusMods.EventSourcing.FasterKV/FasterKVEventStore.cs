using System.Threading.Tasks;
using FASTER.core;
using NexusMods.EventSourcing.Abstractions;

namespace NexusMods.EventSourcing.FasterKV;

public class FasterKVEventStore<TSerializer> : IEventStore
    where TSerializer : IEventSerializer
{
    private readonly FasterKVSettings<SpanByteAndMemory, SpanByteAndMemory> _settings;
    private readonly FasterKV<SpanByteAndMemory,SpanByteAndMemory> _kvStore;


    public FasterKVEventStore(TSerializer serializer, Settings settings)
    {
        _settings = new FasterKVSettings<SpanByteAndMemory, SpanByteAndMemory>(settings.StorageLocation.ToString());
        _kvStore = new FasterKV<SpanByteAndMemory, SpanByteAndMemory>(_settings);
    }

    public ValueTask Add<T>(T eventEntity) where T : IEvent
    {
        throw new System.NotImplementedException();
    }

    public void EventsForEntity<TIngester>(EntityId entityId, TIngester ingester) where TIngester : IEventIngester
    {
        throw new System.NotImplementedException();
    }
}
