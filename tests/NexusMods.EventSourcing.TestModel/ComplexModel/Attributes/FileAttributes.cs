using NexusMods.EventSourcing.Abstractions;
using NexusMods.Paths;

namespace NexusMods.EventSourcing.TestModel.ComplexModel.Attributes;

public class FileAttributes
{
    /// <summary>
    /// The path of the file
    /// </summary>
    public class Path : ScalarAttribute<Path, RelativePath>;

    /// <summary>
    /// The size of the file
    /// </summary>
    public class Size : ScalarAttribute<Size, NexusMods.Paths.Size>;

    /// <summary>
    /// The hashcode of the file
    /// </summary>
    public class Hash : ScalarAttribute<Hash, NexusMods.Hashing.xxHash64.Hash>;

    /// <summary>
    /// The mod this file belongs to
    /// </summary>
    public class ModId : ScalarAttribute<ModId, EntityId>;
}
