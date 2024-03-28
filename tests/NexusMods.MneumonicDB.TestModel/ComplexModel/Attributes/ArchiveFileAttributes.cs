using NexusMods.MneumonicDB.Abstractions;
using NexusMods.Paths;

namespace NexusMods.MneumonicDB.TestModel.ComplexModel.Attributes;

/// <summary>
/// This class intentionally overlaps with the FileAttributes class in the tests, this is to test the attribute collection
/// is able to handle multiple attributes with the same short name (but unique full name).
/// </summary>
public class ArchiveFileAttributes
{
    /// <summary>
    ///     The path of the file in the archive
    /// </summary>
    public class Path() : ScalarAttribute<Path, RelativePath>(isIndexed: true);

    /// <summary>
    ///     The hashcode of the file
    /// </summary>
    public class Hash() : ScalarAttribute<Hash, Hashing.xxHash64.Hash>(isIndexed: true);
}
