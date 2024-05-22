using System.Collections.Generic;

namespace NexusMods.MnemonicDB.Abstractions;

public static class IDbExtensions
{
    /// <summary>
    /// Returns all entities that have all of the given attributes
    /// </summary>
    public static IEnumerable<EntityId> Intersection(this IDb db, params IAttribute[] attrs)
    {

    }
}
