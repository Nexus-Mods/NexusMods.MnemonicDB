using System;
using System.Threading.Tasks;
using NexusMods.MnemonicDB.Abstractions;

namespace MnemonicDB.Query;

public interface IFact
{
    /// <summary>
    /// Returns the number of items int he fact tuple
    /// </summary>
    public int Arity { get; }
    
    /// <summary>
    /// The name of the fact
    /// </summary>
    public Symbol Predicate { get; }
}


public readonly record struct Fact<TA>(Symbol Predicate, Term<TA> A) : IFact
{
    public int Arity => 1;
}

public readonly record struct Fact<TA, TB>(Symbol Predicate, Term<TA> A, Term<TB> B) : IFact
{
    public int Arity => 2;
}

public readonly record struct Fact<TA, TB, TC>(Symbol Predicate, Term<TA> A, Term<TB> B, Term<TC> C) : IFact
{
    public int Arity => 3;
}

public readonly record struct Fact<TA, TB, TC, TD>(Symbol Predicate, Term<TA> A, Term<TB> B, Term<TC> C, Term<TD> D) : IFact
{
    public int Arity => 4;
}

public readonly record struct Fact<TA, TB, TC, TD, TE>(Symbol Predicate, Term<TA> A, Term<TB> B, Term<TC> C, Term<TD> D, Term<TE> E) : IFact
{
    public int Arity => 5;
}
