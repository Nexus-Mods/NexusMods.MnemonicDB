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

}
