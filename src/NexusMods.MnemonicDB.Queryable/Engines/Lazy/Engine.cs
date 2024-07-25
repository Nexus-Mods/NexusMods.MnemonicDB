using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using NexusMods.MnemonicDB.Queryable.AbstractSyntaxTree;
using NexusMods.MnemonicDB.Queryable.KnowledgeDatabase;
using NexusMods.MnemonicDB.Queryable.TypeSystem;

namespace NexusMods.MnemonicDB.Queryable.Engines.Lazy;
using Env = System.Collections.Immutable.ImmutableDictionary<NexusMods.MnemonicDB.Queryable.AbstractSyntaxTree.ILVar, object>;

public static class EngineExtensions
{
    
}

public class Engine
{
    private readonly Definitions _definitions;

    public Engine(Definitions definitions)
    {
        _definitions = definitions;
    }
    
    public Func<TIn1, IEnumerable<TOut1>> ToLazy<TIn1, TOut1>(BuiltQuery<ArgTuple<TIn1>, ArgTuple<TOut1>> query)
    {
        return Compile(query);
    }
    
    public Func<TIn1, IEnumerable<TOut1>> Compile<TIn1, TOut1>(BuiltQuery<ArgTuple<TIn1>, ArgTuple<TOut1>> query)
    {
        HashSet<ILVar> bound = new();
        bound.Add(query.FromArgs[0]);

        var fromPredicates = from p in query.Predicates
            from a in p.Arguments
            where a.IsVariable
            select a.Variable;

        var allLVars = query.FromArgs
            .Concat(query.SelectArgs)
            .Concat(fromPredicates)
            .Distinct()
            .Select(KeyValuePair.Create)
            .ToDictionary();

        var steps = new List<Func<ILVarBox[], bool>>();
        
        foreach (var predicate in query.Predicates)
        {
            var definitions = predicate.Lookup(_definitions);
            var definition = definitions.First();
            var stepper = MakeStepper(definition, predicate);
            
            steps.Add(stepper);
            
            foreach (var newlyBound in predicate.Arguments.Where(a => a.IsVariable).Select(a => a.Variable))
                bound.Add(newlyBound);
            
        }
        
        var stateMachine = new StateMachine
        {
            Variables = allLVars,
            Slots = allLVars.Keys.ToArray(),
            Steppers = steps.ToArray()
        };

        return (TIn1 in1) =>
        {
            return stateMachine.Build<TIn1, TOut1>(in1, (LVar<TIn1>)query.FromArgs[0], (LVar<TOut1>)query.SelectArgs[0]);
        };
    }

    public Func<ILVarBox[], bool> MakeStepper(IPredicateDefinition definition, IPredicate predicate)
    {
        if (definition.Arity != predicate.Arity)
            throw new InvalidOperationException("Arity mismatch");
        
    }

}
