using NexusMods.EventSourcing.Abstractions.ModelGeneration;

namespace NexusMods.EventSourcing.TestModel.Model;

[ModelDefinition]
public static partial class Mod
{
    public static AttributeDefinitions AttributeDefinitions = new AttributeDefinitionsBuilder()
        .Define<string>("Name", "The name of the mod")
        .Define<bool>("Enabled", "Whether the mod is enabled")
        .Build();

}
