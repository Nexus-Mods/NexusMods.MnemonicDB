using System.Collections.Generic;
using NexusMods.EventSourcing.Abstractions;

namespace NexusMods.EventSourcing.Storage;

public static class AttributeRegistryExtensions
{
    /// <summary>
    /// Converts the datoms to typed datoms using the given registry.
    /// </summary>
    /// <param name="datoms"></param>
    /// <param name="registry"></param>
    /// <returns></returns>
    public static IEnumerable<IReadDatom> Typed(this IEnumerable<Datom> datoms, AttributeRegistry registry)
    {
        foreach (var datom in datoms)
        {
            yield return registry.Resolve(datom);
        }
    }

}
