using NexusMods.EventSourcing.Abstractions.ModelGeneration;

namespace NexusMods.EventSourcing.TestModel.Model;

[ModelDefinition]
public static partial class Loadout
{
    public static AttributeDefinitions AttributeDefinitions = new AttributeDefinitionsBuilder()
        .Define<string>("Name", "The name of the loadout")
        .Build();

}
