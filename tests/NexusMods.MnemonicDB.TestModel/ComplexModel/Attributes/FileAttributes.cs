using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Paths;

namespace NexusMods.MnemonicDB.TestModel.ComplexModel.Attributes;

public class FileAttributes
{
    /// <summary>
    ///     The path of the file
    /// </summary>
    public class Path() : Attribute<Path, RelativePath>(isIndexed: true);

    /// <summary>
    ///     The size of the file
    /// </summary>
    public class Size : Attribute<Size, Paths.Size>;

    /// <summary>
    ///     The hashcode of the file
    /// </summary>
    public class Hash() : Attribute<Hash, Hashing.xxHash64.Hash>(isIndexed: true);

    /// <summary>
    ///     The mod this file belongs to
    /// </summary>
    public class ModId : Attribute<ModId, EntityId>;
}
