namespace NexusMods.EventSourcing.Storage.Columns.ULongColumns;

/// <summary>
/// Classes that do not exist as a contiguous block of memory, but rather as a packed representation,
/// will implement this interface.
/// </summary>
public interface IPacked : IReadable {

}
