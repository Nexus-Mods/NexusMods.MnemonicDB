namespace NexusMods.EventSourcing.Abstractions.Columns.BlobColumns;

/// <summary>
/// A packed blob column. This column does not allow appending, but is packed in a more efficent format
/// </summary>
public interface IPacked : IReadable
{

}
