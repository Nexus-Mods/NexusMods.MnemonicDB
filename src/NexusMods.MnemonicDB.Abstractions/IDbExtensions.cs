using System.Collections.Generic;
using NexusMods.MnemonicDB.Abstractions.IndexSegments;

namespace NexusMods.MnemonicDB.Abstractions;

public static class IDbExtensions
{
    /// <summary>
    /// Returns all entities that have all the given attributes
    /// </summary>
    public static IEnumerable<EntityId> Intersection(this IDb db, params IAttribute[] attrs)
    {
        static bool Contains(IndexSegment segment, AttributeId a)
        {
            for (var i = 0; i < segment.Count; i++)
            {
                if (segment[i].A == a)
                {
                    return true;
                }
            }
            return false;
        }

        foreach (var id in db.Find(attrs[0]))
        {
            var index = db.Get(id);
            var found = true;
            for (var i = 1; i < attrs.Length; i++)
            {
                if (Contains(index, attrs[i].GetDbId(db.Registry.Id)))
                    continue;

                found = false;
                break;
            }

            if (found)
                yield return id;
        }

    }
}
