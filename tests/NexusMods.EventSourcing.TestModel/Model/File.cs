using NexusMods.EventSourcing.Abstractions.ModelGeneration;

namespace NexusMods.EventSourcing.TestModel.Model;

[ModelDefinition]
public static partial class File
{
    public static AttributeDefinitions Attributes = new AttributeDefinitionsBuilder()
        .Define<string>("Path", "The path of the file")
        .Define<ulong>("Hash", "The hash of the file")
        .Define<ulong>("Index", "A index value for testing purposes")
        .Build();

}
