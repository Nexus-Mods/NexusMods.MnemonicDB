using System;
using System.Collections.Generic;

namespace NexusMods.Query.Abstractions;

/// <summary>
/// Facts are the smallest unit of information in the database. They are most often tuple-like structures
/// </summary>
public interface IFact
{
    public Func<object[], IEnumerable<object[]>> MakeLazy(Dictionary<ILVar, int> lvars, HashSet<ILVar> bound);
}


/// <summary>
/// A typed fact, with one field
/// </summary>
public interface IFact<TA> : IFact
{
    
}

/// <summary>
/// A typed fact, with two fields
/// </summary>
public interface IFact<TA, TB> : IFact
{
    
}

/// <summary>
/// A typed fact, with three fields
/// </summary>
public interface IFact<TA, TB, TC> : IFact where TA : notnull where TB : notnull where TC : notnull
{
    public void Bind(Term<TA> a, Term<TB> b, Term<TC> c);
}
