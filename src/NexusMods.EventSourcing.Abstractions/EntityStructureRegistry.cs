using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography.X509Certificates;

namespace NexusMods.EventSourcing.Abstractions;

/// <summary>
/// Contains structure information about entities (what attributes they have, etc).
/// </summary>
public static class EntityStructureRegistry
{
    private static readonly ConcurrentDictionary<Type, ConcurrentDictionary<string, IAttribute>> _entityStructures = new();


    private static readonly ConcurrentDictionary<Type, EntityDefinition> _entityDefinitionsByType = new();
    private static readonly ConcurrentDictionary<UInt128, EntityDefinition> _entityDefinitionsByUUID = new();

    /// <summary>
    /// Register an attribute in the global registry.
    /// </summary>
    /// <param name="attribute"></param>
    public static void Register(IAttribute attribute)
    {
        TOP:
        if (_entityStructures.TryGetValue(attribute.Owner, out var found))
        {
            found.TryAdd(attribute.Name, attribute);
            return;
        }

        var dict = new ConcurrentDictionary<string, IAttribute>();
        dict.TryAdd(attribute.Name, attribute);
        if (!_entityStructures.TryAdd(attribute.Owner, dict))
        {
            goto TOP;
        }
    }

    /// <summary>
    /// Registers an entity type in the global registry.
    /// </summary>
    /// <param name="type"></param>
    /// <param name="guid"></param>
    /// <param name="revison"></param>
    public static void Register(EntityDefinition definition)
    {
        _entityDefinitionsByType.TryAdd(definition.Type, definition);
        _entityDefinitionsByUUID.TryAdd(definition.UUID, definition);
    }

    /// <summary>
    /// Returns all attributes for the given entity type.
    /// </summary>
    /// <param name="owner"></param>
    /// <returns></returns>
    public static bool TryGetAttributes(Type owner, [NotNullWhen(true)] out ConcurrentDictionary<string, IAttribute>? result)
    {
        if (_entityStructures.TryGetValue(owner, out var found ))
        {
            result = found;
            return true;
        }

        result = default!;
        return false;
    }

    /// <summary>
    /// Gets the entity definition for the given C# type.
    /// </summary>
    /// <typeparam name="TType"></typeparam>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public static EntityDefinition GetDefinition<TType>() where TType : IEntity
    {
        if (_entityDefinitionsByType.TryGetValue(typeof(TType), out var result))
        {
            return result;
        }

        throw new InvalidOperationException($"No entity definition found for type {typeof(TType).Name}");
    }

    /// <summary>
    /// Gets the entity definition for the given UUID.
    /// </summary>
    /// <param name="uuid"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public static EntityDefinition GetDefinitionByUUID(UInt128 uuid)
    {
        if (_entityDefinitionsByUUID.TryGetValue(uuid, out var result))
        {
            return result;
        }

        throw new InvalidOperationException($"No entity definition found for UUID {uuid}");
    }
}
