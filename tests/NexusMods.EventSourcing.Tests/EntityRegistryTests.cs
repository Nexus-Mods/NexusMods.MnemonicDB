using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.TestModel.Model;

namespace NexusMods.EventSourcing.Tests;

public class EntityRegistryTests(IEnumerable<EntityDefinition> entityDefinitions)
{
    [Fact]
    public void EntityRegistry_ContainsEntityDefinitions()
    {
        var entityDefinitionsArray = entityDefinitions.ToArray();
        entityDefinitionsArray.Should().NotBeEmpty();
        entityDefinitionsArray.Should().ContainSingle(x => x.EntityType == typeof(Mod));

        var mod = entityDefinitionsArray.Single(x => x.EntityType == typeof(Mod));
        mod.Attributes.Should().NotBeEmpty();
        mod.Attributes.Should().ContainSingle(x => x.Name == "Name" && x.NativeType == typeof(string));
    }
}
