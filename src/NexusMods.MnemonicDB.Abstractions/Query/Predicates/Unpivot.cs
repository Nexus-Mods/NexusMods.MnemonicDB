using System.Collections.Generic;
using System.Collections.Immutable;

namespace NexusMods.MnemonicDB.Abstractions.Query.Predicates;

/// <summary>
/// Unpivots a collection of values into a single lvar, esentially flattening the collection
/// into a series of results.
/// </summary>
public record Unpivot<T> : Predicate
{
    /// <summary>
    /// The source collection to unpivot.
    /// </summary>
    public readonly Term<IEnumerable<T>> Source;
    
    /// <summary>
    /// The destination term to assign the unpivoted values.
    /// </summary>
    public readonly Term<T> Destination;

    internal Unpivot(Term<IEnumerable<T>> src, Term<T> dest)
    {
        Source = src;
        Destination = dest;
    }
    
    /// <inheritdoc />
    public override IEnumerable<(string Name, ITerm Term)> Terms
    {
        get
        {
            yield return (nameof(Source), Source);
            yield return (nameof(Destination), Destination);
        }
    }

    public override IEnumerable<ImmutableDictionary<LVar, object>> Apply(IEnumerable<ImmutableDictionary<LVar, object>> envStream)
    {
        foreach (var env in envStream)
        {
            if (!env.TryGetValue(Source.LVar, out var srcVal))
            {
                continue;
            }

            foreach (var val in (IEnumerable<T>)srcVal)
            {
                yield return env.Add(Destination.LVar, val!);
            }
        }
    }
}
