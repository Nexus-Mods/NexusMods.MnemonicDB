using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Abstractions.Models;
using NexusMods.EventSourcing.TestModel.Model.Attributes;

namespace NexusMods.EventSourcing.TestModel.Model;

public class ArchiveFile(ITransaction? tx) : AReadModel<ArchiveFile>(tx)
{
    /// <summary>
    ///     Base attribute
    /// </summary>
    [From<ModFileAttributes.Path>]
    public required string ModPath { get; init; }

    /// <summary>
    ///     Base attribute
    /// </summary>
    [From<ArchiveFileAttributes.Path>]
    public required string Path { get; init; }

    /// <summary>
    ///     Example of "inheritance" of attributes from other namespaces
    /// </summary>
    [From<ArchiveFileAttributes.ArchiveHash>]
    public required ulong Hash { get; init; }

    /// <summary>
    ///     The index of the file in the archive used for debugging purposes
    /// </summary>
    [From<ModFileAttributes.Index>]
    public required ulong Index { get; init; }
}
