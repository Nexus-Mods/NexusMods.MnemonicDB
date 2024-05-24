using System.Collections.Generic;

namespace NexusMods.MnemonicDB.Abstractions;

public static class IDbExtensions
{
    /// <summary>
    /// Returns all entities that have all of the given attributes
    /// </summary>
    public static IEnumerable<EntityId> Intersection(this IDb db, params IAttribute[] attrs)
    {
        foreach (var id in db.Find(attrs[0]))
        {
            var index = db.Get(id);
            for (int i = 1; i < attrs.Length; i++)
            {
                for (int j = 0; j < index.Count; j++)
                {
                    if (index[j] = attrs[i].
                    {
                        yield return id;
                    }
                }
            }
        }

    }
}
