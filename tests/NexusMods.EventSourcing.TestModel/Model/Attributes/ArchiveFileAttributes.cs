using NexusMods.EventSourcing.Abstractions;

namespace NexusMods.EventSourcing.TestModel.Model.Attributes;

public static class ArchiveFileAttributes
{
    /// <summary>
    /// Extra attribute with a different name
    /// </summary>
    public class ArchiveHash : ScalarAttribute<ArchiveHash, ulong>;

    /// <summary>
    /// Overlapping name with ModFileAttributes.Path
    /// </summary>
    public class Path : ScalarAttribute<Path, string>;

}
