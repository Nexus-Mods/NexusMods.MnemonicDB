using System;
using System.Collections.Generic;
using System.Linq;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Queryable.Engines;

namespace NexusMods.MnemonicDB.Queryable.KnowledgeDatabase;

public class Definitions
{
    
    private ILookup<(Symbol, int), IPredicateDefinition> _predicates;
    
    public Definitions(IEnumerable<IPredicateDefinition> definitions)
    {
        _predicates = definitions.ToLookup(t => (t.Name, t.Arity));

    }
    
    /// <summary>
    /// Gets all the predicates of name/2
    /// </summary>
    public IEnumerable<IPredicateDefinition<T1, T2>> Predicates<T1, T2>(Symbol name)
    {
        return ConformingTo<T1, T2>(_predicates[(name, 2)]);
    }

    private IEnumerable<IPredicateDefinition<T1,T2>> ConformingTo<T1, T2>(IEnumerable<IPredicateDefinition> predicates)
    {
        foreach (var predicate in predicates)
        {
            switch (predicate)
            {
                case IPredicateDefinition<T1, T2> casted:
                    yield return casted;
                    break;
                case IPredicateDefinitionFactory factory:
                {
                    if (factory.TrySpecialize<T1, T2>(out var specialized))
                        yield return specialized;
                    break;
                }
            }
        }
    }
}
