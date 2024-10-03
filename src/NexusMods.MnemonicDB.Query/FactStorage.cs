using System;
using System.Collections.Generic;
using System.Numerics;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.MnemonicDB.Query;

public class FactStorage
{
    private Dictionary<FactKey, object> _facts = new();
    
    /// <summary>
    /// Inserts a fact into the storage
    /// </summary>
    public void Insert<TFact>(TFact fact) where TFact : IFact
    {
        var key = new FactKey(typeof(TFact), fact.Predicate);
        GetOrAdd<TFact>(key).Add(fact);
    }
    
    public void Insert<TArg1>(Symbol predicate, TArg1 arg1) where TArg1 : IEquatable<TArg1>
    {
        Insert(Fact.Create(predicate, arg1));
    }
    
    public void Insert<TArg1, TArg2>(Symbol predicate, TArg1 arg1, TArg2 arg2)
        where TArg1 : IEquatable<TArg1> where TArg2 : IEquatable<TArg2>
    {
        Insert(Fact.Create(predicate, arg1, arg2));
    }
    
    public void Insert<TArg1, TArg2, TArg3>(Symbol predicate, TArg1 arg1, TArg2 arg2, TArg3 arg3) 
        where TArg1 : IEquatable<TArg1> where TArg2 : IEquatable<TArg2> where TArg3 : IEquatable<TArg3>
    {
        Insert(Fact.Create(predicate, arg1, arg2, arg3));
    }
    
    public void Insert<TArg1, TArg2, TArg3, TArg4>(Symbol predicate, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4) 
        where TArg1 : IEquatable<TArg1> where TArg2 : IEquatable<TArg2> where TArg3 : IEquatable<TArg3> where TArg4 : IEquatable<TArg4>
    {
        Insert(Fact.Create(predicate, arg1, arg2, arg3, arg4));
    }

    /// <summary>
    /// Gets all facts that match the goal
    /// </summary>
    public IEnumerable<Fact<TArg1>> Get<TArg1>(Goal<TArg1> goal) 
        where TArg1 : IEquatable<TArg1>
    {
        var key = new FactKey(typeof(Fact<TArg1>), goal.Predicate);
        var facts = GetOrAdd<Fact<TArg1>>(key);
        foreach (var fact in facts)
        {
            if (goal.Matches(fact))
            {
                yield return fact;
            }
        }
    }
    
    /// <summary>
    /// Gets all facts that match the goal
    /// </summary>
    public IEnumerable<Fact<TArg1, TArg2>> Get<TArg1, TArg2>(Goal<TArg1, TArg2> goal) 
        where TArg1 : IEquatable<TArg1>
        where TArg2 : IEquatable<TArg2> 
    {
        var key = new FactKey(typeof(Fact<TArg1, TArg2>), goal.Predicate);
        var facts = GetOrAdd<Fact<TArg1, TArg2>>(key);
        foreach (var fact in facts)
        {
            if (goal.Matches(fact))
            {
                yield return fact;
            }
        }
    }
    
    public IEnumerable<Fact<TArg1, TArg2, TArg3>> Get<TArg1, TArg2, TArg3>(Goal<TArg1, TArg2, TArg3> goal) 
        where TArg1 : IEquatable<TArg1>
        where TArg2 : IEquatable<TArg2>
        where TArg3 : IEquatable<TArg3>
    {
        var key = new FactKey(typeof(Fact<TArg1, TArg2, TArg3>), goal.Predicate);
        var facts = GetOrAdd<Fact<TArg1, TArg2, TArg3>>(key);
        foreach (var fact in facts)
        {
            if (goal.Matches(fact))
            {
                yield return fact;
            }
        }
    }
    
    private HashSet<TFact> GetOrAdd<TFact>(FactKey key) where TFact : IFact
    {
        if (_facts.TryGetValue(key, out var fact))
        {
            return (HashSet<TFact>)fact;
        }
        var newFacts = new HashSet<TFact>();
        _facts[key] = newFacts;
        return newFacts;
    }
}
