using System.Collections.Generic;

namespace NexusMods.EventSourcing.Abstractions;

/// <summary>
/// Extensions for <see cref="IEnumerable{T}"/>.
/// </summary>
public static class IEnumerableExtensions
{

    /// <summary>
    /// Unpacks the datoms in the given sequence returning typed versions of them.
    /// </summary>
    public static IEnumerable<IReadDatom> Typed(this IEnumerable<Datom> datoms, IDatomStore store)
    {
        return store.Resolved(datoms);
    }

}
