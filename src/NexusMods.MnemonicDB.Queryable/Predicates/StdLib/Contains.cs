using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Queryable.AbstractSyntaxTree;
using NexusMods.MnemonicDB.Queryable.Engines;
using NexusMods.MnemonicDB.Queryable.KnowledgeDatabase;
using NexusMods.MnemonicDB.Queryable.TypeSystem;

namespace NexusMods.MnemonicDB.Queryable.Predicates.StdLib;

public static class ContainsExtensions
{
    internal static readonly Symbol ContainsSymbol = Symbol.Intern("NexusMods.MnemonicDB.Queryable.Predicates.StdLib/Contains");
    
    public static QueryBuilder<TArgs> Contains<TArgs, T>(this QueryBuilder<TArgs> qb, Argument<IEnumerable<T>> argument, LVar<T> toVar, [CallerArgumentExpression("toVar")] string toVarName = "?")
        where T : notnull where TArgs : IArgTupe
    {
        return qb.With(new Predicate<IEnumerable<T>, T>(ContainsSymbol, argument, toVar));
    }
}

public class Contains<T> : IPredicateDefinition<IEnumerable<T>, T>
{
    public bool Supports(ArgumentType type1, ArgumentType type2)
    {
        return type1 != ArgumentType.Unbound;
    }

    public bool Execute(IEnumerable<T> arg1, T arg2)
    {
        return arg1.Contains(arg2);
    }

    public async ValueTask Predicate_Bound_Unbound<TEmitter>(IEnumerable<T> arg1, TEmitter arg2) where TEmitter : IEmitter<T>
    {
        foreach (var item in arg1)
        {
            await arg2.Emit(item);
        }
    }

    public Symbol Name => ContainsExtensions.ContainsSymbol;
    public int Arity => 2;
    
    public Func<ILVar[], bool> MakeStepper(Dictionary<ILVar, int> indexes)
    {
        
    }
}
