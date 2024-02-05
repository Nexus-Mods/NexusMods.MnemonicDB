using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using NexusMods.EventSourcing.Abstractions;

namespace NexusMods.EventSourcing;

/// <summary>
/// Organizes and gives access to the entity definitions and attribute types and definitions. Essentially all the
/// metadata and reflected information about the entities and their attributes.
/// </summary>
public class EntityRegistry : IEntityRegistry
{
    private readonly IAttributeType[] _attributeTypes;
    private readonly EntityDefinition[] _entityDefinitions;
    private AttributeDefinition[] _attributes;

    /// <summary>
    /// DI constructor
    /// </summary>
    /// <param name="entityDefinitions"></param>
    /// <param name="attributeTypes"></param>
    public EntityRegistry(IEnumerable<EntityDefinition> entityDefinitions, IEnumerable<IAttributeType> attributeTypes)
    {
        _attributeTypes = attributeTypes.ToArray();
        _entityDefinitions = entityDefinitions.ToArray();

        var attributes = new List<AttributeDefinition>();

        foreach (var entityDefinition in _entityDefinitions)
        {
            foreach (var attribute in entityDefinition.Attributes)
            {
                var attributeType = _attributeTypes.Single(x => x.DomainType == attribute.NativeType);
                attributes.Add(attributeType.Construct(entityDefinition.Id, attribute.Name));
            }
        }

        _attributes = attributes.ToArray();
    }

}
