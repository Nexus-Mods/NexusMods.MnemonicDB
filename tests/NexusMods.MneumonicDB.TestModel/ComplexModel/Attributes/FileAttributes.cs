﻿using NexusMods.MneumonicDB.Abstractions;
using NexusMods.Paths;

namespace NexusMods.MneumonicDB.TestModel.ComplexModel.Attributes;

public class FileAttributes
{
    /// <summary>
    ///     The path of the file
    /// </summary>
    public class Path() : ScalarAttribute<Path, RelativePath>(isIndexed: true);

    /// <summary>
    ///     The size of the file
    /// </summary>
    public class Size : ScalarAttribute<Size, Paths.Size>;

    /// <summary>
    ///     The hashcode of the file
    /// </summary>
    public class Hash() : ScalarAttribute<Hash, Hashing.xxHash64.Hash>(isIndexed: true);

    /// <summary>
    ///     The mod this file belongs to
    /// </summary>
    public class ModId : ScalarAttribute<ModId, EntityId>;
}
