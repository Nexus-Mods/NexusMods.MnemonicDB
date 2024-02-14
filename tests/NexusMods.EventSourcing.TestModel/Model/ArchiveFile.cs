using NexusMods.EventSourcing.Abstractions.ModelGeneration;

namespace NexusMods.EventSourcing.TestModel.Model;

[ModelDefinition]
public static partial class ArchiveFile
{
    public static AttributeDefinitions Attributes = new AttributeDefinitionsBuilder()
        .Include<File.Hash>()
        .Include<File.Path>()
        .Define<ulong>("Index", "A index value for testing purposes")
        .Define<string>("ArchivePath", "The path of the archive file")
        .Build();

}
