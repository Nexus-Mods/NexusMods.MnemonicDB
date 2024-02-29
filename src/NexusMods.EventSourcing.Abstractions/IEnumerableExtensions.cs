using System.Collections.Generic;

namespace NexusMods.EventSourcing.Abstractions;

public static class IEnumerableExtensions
{

    public static IEnumerable<IReadDatom> Typed(this IEnumerable<Datom> datoms, IDatomStore store)
    {
        return store.Resolved(datoms);
    }

}
