using System;
using System.Collections.Generic;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Queryable.Engines;
using NexusMods.MnemonicDB.Queryable.TypeSystem;

namespace NexusMods.MnemonicDB.Queryable.KnowledgeDatabase;

public interface IPredicate
{
    public int Arity { get; }
    public IEnumerable<IArgument> Arguments { get; }
    
    public IEnumerable<IPredicateDefinition> Lookup(Definitions definitions);
}


public class Predicate<TArg1, TArg2> : IPredicate 
    where TArg1 : notnull 
    where TArg2 : notnull
{
    private readonly Symbol _name;
    private readonly Argument<TArg1> _a1;
    private readonly Argument<TArg2> _a2;
    
    public int Arity => 2;
    public IEnumerable<IArgument> Arguments => new IArgument[] { _a1, _a2 };

    public Predicate(Symbol name, Argument<TArg1> a1, Argument<TArg2> a2)
    {
        _name = name;
        _a1 = a1;
        _a2 = a2;
    }
    
    public IEnumerable<IPredicateDefinition> Lookup(Definitions definitions)
    {
        return definitions.Predicates<TArg1, TArg2>(_name);
    }

    public override string ToString()
    {
        return $"{_name.Name}({_a1.ToString()}, {_a2.ToString()})";
    }
}

