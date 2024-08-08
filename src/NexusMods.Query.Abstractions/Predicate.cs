using System;
using System.Collections.Generic;
using NexusMods.Query.Abstractions.Engines;
using NexusMods.Query.Abstractions.Engines.TopDownLazy;

namespace NexusMods.Query.Abstractions;


public class Predicate<TFact, TA>(Term<TA> a) : IPredicate 
    where TFact : IFact<TA> where TA : notnull
{
    public void RegisterLVars(HashSet<ILVar> lvars)
    {
        a.RegisterLVar(lvars);
    }

    public Func<IEnumerable<ILVarBox[]>, IEnumerable<ILVarBox[]>> MakeLazy(Context context)
    {
        throw new NotImplementedException();
    }
}

public class Predicate<TFact, TA, TB>(Term<TA> A, Term<TB> B) : IPredicate 
    where TFact : IFact<TA, TB> where TA : notnull where TB : notnull
{
    public void RegisterLVars(HashSet<ILVar> lvars)
    {
        A.RegisterLVar(lvars);
        B.RegisterLVar(lvars);
    }

    public Func<IEnumerable<ILVarBox[]>, IEnumerable<ILVarBox[]>> MakeLazy(Context context)
    {
        throw new NotImplementedException();
    }
}

public record Predicate<TFact, TA, TB, TC>(Term<TA> A, Term<TB> B, Term<TC> C) : IPredicate 
    where TFact : IFact<TA, TB, TC> where TA : notnull where TB : notnull where TC : notnull
{
    public void RegisterLVars(HashSet<ILVar> lvars)
    {
        A.RegisterLVar(lvars);
        B.RegisterLVar(lvars);
        C.RegisterLVar(lvars);
    }
    



    public Func<IEnumerable<ILVarBox[]>, IEnumerable<ILVarBox[]>> MakeLazy(Context context)
    {
        var (aBound, aIdx) = context.Resolve(A);
        var (bBound, bIdx) = context.Resolve(B);
        var (cBound, cIdx) = context.Resolve(C);

        switch ((aBound, bBound, cBound))
        {
            case (ResolveType.Unbound, ResolveType.Constant, ResolveType.Constant):
                return TFact.MakeLazyUCC(context, aIdx, B.Constant.Value, C.Constant.Value);
            case (ResolveType.LVar, ResolveType.Constant, ResolveType.Unbound):
                return TFact.MakeLazyLCU(context, aIdx, B.Constant.Value, cIdx);
            case (ResolveType.LVar, ResolveType.Constant, ResolveType.Constant):
                return TFact.MakeLazyLCC(context, aIdx, B.Constant.Value, C.Constant.Value);
            
            default:
                throw new Exception($"Invalid Bindings for Predicate, got {aBound}, {bBound}, {cBound}");
        }
    }
}
