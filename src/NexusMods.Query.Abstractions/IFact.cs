using System;
using System.Collections.Generic;
using NexusMods.Query.Abstractions.Engines;
using NexusMods.Query.Abstractions.Engines.TopDownLazy;

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
    static abstract void MakeLazyUCC<TEmitter>(Context context, TEmitter emitter, TB cB, TC cC) 
        where TEmitter : IEmitter<TA>;
    
    static abstract Func<IEnumerable<ILVarBox[]>, IEnumerable<ILVarBox[]>> MakeLazyLCU(Context context, int aIdx, TB cB, int cIdx);
    static abstract Func<IEnumerable<ILVarBox[]>, IEnumerable<ILVarBox[]>> MakeLazyLCC(Context context, int aIdx, TB cB, TC cC);
}
