using NexusMods.MnemonicDB.Abstractions.Models;
using NexusMods.MnemonicDB.TestModel.Attributes;

namespace NexusMods.MnemonicDB.TestModel;

/// <summary>
/// A file that comes from an archive
/// </summary>
[Include<File>]
public partial class ArchiveFile : IModelDefinition
{
    private const string Namespace = "NexusMods.MnemonicDB.TestModel.ArchiveFile";
    /// <summary>
    /// The path of the file in the archive
    /// </summary>
    public static readonly RelativePathAttribute Path = new(Namespace, nameof(Path)) { IsIndexed = true };
    /// <summary>
    /// The hash of the file in the archive
    /// </summary>
    public static readonly HashAttribute Hash = new(Namespace, nameof(Hash));
}
