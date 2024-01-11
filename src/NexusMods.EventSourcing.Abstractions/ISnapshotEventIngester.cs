namespace NexusMods.EventSourcing.Abstractions;

/// <summary>
/// An event ingester that supports entity snapshots
/// </summary>
public interface ISnapshotEventIngester : IEventIngester
{
    /// <summary>
    /// This method will be called for each attribute snapshotted, before the normal event ingestion is called
    /// </summary>
    /// <param name="attributeName"></param>
    /// <param name="attribute"></param>
    public void IngestSnapshotAttribute(string attributeName, IAttribute attribute);
}
