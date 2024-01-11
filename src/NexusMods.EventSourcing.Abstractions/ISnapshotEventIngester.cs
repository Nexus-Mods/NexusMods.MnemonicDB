namespace NexusMods.EventSourcing.Abstractions;

/// <summary>
/// An event ingester that supports entity snapshots
/// </summary>
public interface ISnapshotEventIngester : IEventIngester
{

    /// <summary>
    /// Ingests a snapshot of an entity, a false return value means the snapshot is invalid
    /// and the entity should be rebuilt from scratch (by replaying all events)
    /// </summary>
    /// <param name="definition"></param>
    /// <param name="attributes"></param>
    /// <returns></returns>
    public bool IngestSnapshot(EntityDefinition definition,
        (IAttribute Attribute, IAccumulator Accumulator)[] attributes);

}
