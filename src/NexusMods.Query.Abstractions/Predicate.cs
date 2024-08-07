using System.Collections.Generic;

namespace NexusMods.Query.Abstractions;


public class Predicate<TFact, TA>(Term<TA> a) : IPredicate 
    where TFact : IFact<TA> where TA : notnull
{
    public void RegisterLVars(HashSet<ILVar> lvars)
    {
        a.RegisterLVar(lvars);
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
}
