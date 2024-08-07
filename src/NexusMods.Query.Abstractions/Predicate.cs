namespace NexusMods.Query.Abstractions;


public class Predicate<TFact, TA> : IPredicate 
    where TFact : IFact<TA>
{
    
}

public class Predicate<TFact, TA, TB> : IPredicate 
    where TFact : IFact<TA, TB>
{
    
}

public record Predicate<TFact, TA, TB, TC>(Term<TA> A, Term<TB> B, Term<TC> C) : IPredicate 
    where TFact : IFact<TA, TB, TC>
{
    
}
