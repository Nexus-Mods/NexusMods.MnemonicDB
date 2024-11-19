using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace NexusMods.MnemonicDB.Abstractions.Query;

/// <summary>
/// An abstract predicate. A Predicate is a logical statement, a bit like a function
/// call, that provides a pattern against which data is matched
/// </summary>
public abstract record Predicate
{
    /// <summary>
    /// The terms that make up the predicate
    /// </summary>
    public abstract IEnumerable<(string Name, ITerm Term)> Terms { get; }

    public abstract IEnumerable<ImmutableDictionary<LVar, object>> Apply(IEnumerable<ImmutableDictionary<LVar, object>> envStream);
    
    public ImmutableArray<LVar> EnvironmentEnter { get; init; } = ImmutableArray<LVar>.Empty;
    public ImmutableArray<LVar> EnvironmentExit { get; init; }= ImmutableArray<LVar>.Empty;
    public ImmutableHashSet<LVar> Inputs { get; init; } = ImmutableHashSet<LVar>.Empty;
    public ImmutableHashSet<LVar> Outputs { get; init; } = ImmutableHashSet<LVar>.Empty;
    
    public Predicate Bind(ImmutableArray<LVar> existing)
    {
        var lvars = Terms.Where(t => t.Term.IsLVar)
            .Select(t => t.Term.LVar)
            .ToHashSet();
        return this with
        {
            Inputs = lvars.Intersect(existing).ToImmutableHashSet(),
            Outputs = lvars.Except(existing).ToImmutableHashSet(),
            EnvironmentEnter = existing,
            EnvironmentExit = [..existing.Union(lvars)]
        };
    }

    public Predicate Clean(HashSet<LVar> required)
    {
        var lvars = Terms.Where(t => t.Term.IsLVar)
            .Select(t => t.Term.LVar)
            .ToHashSet();


        var newNode =  this with
        {
            EnvironmentEnter = [..EnvironmentEnter.Where(Inputs.Contains)],
            EnvironmentExit = [..EnvironmentExit.Where(required.Contains)],
        };
        
        
        foreach (var lvar in lvars)
            required.Add(lvar);

        return newNode;

    }
}
