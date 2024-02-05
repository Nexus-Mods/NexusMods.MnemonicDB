using DuckDB.NET;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.TestModel.Model;

namespace NexusMods.EventSourcing.Tests;

public class EntityRegistryTests
{
    private readonly IEnumerable<EntityDefinition> _entityDefinitions;
    private readonly IDatomStore _datomStore;

    public EntityRegistryTests(IEnumerable<EntityDefinition> entityDefinitions, IDatomStore datomStore, IEntityRegistry registry)
    {
        _entityDefinitions = entityDefinitions;
        _datomStore = datomStore;
    }


    [Fact]
    public void EntityRegistry_ContainsEntityDefinitions()
    {
        var entityDefinitionsArray = _entityDefinitions.ToArray();
        entityDefinitionsArray.Should().NotBeEmpty();
        entityDefinitionsArray.Should().ContainSingle(x => x.EntityType == typeof(Mod));

        var mod = entityDefinitionsArray.Single(x => x.EntityType == typeof(Mod));
        mod.Attributes.Should().NotBeEmpty();
        mod.Attributes.Should().ContainSingle(x => x.Name == "Name" && x.NativeType == typeof(string));
    }
}
