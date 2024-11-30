using System;
using System.Collections.Generic;
using System.Linq;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.QueryEngine.Facts;
using NexusMods.MnemonicDB.QueryEngine.Ops;

namespace NexusMods.MnemonicDB.QueryEngine;

public record Annotations
{
    public required LVar[] EnvironmentEnter { get; set; }
    public required LVar[] EnvironmentExit { get; set; }
    public required LVar[] LVars { get; set; }
    public required LVar[] JoinColumns { get; set; }
    public required LVar[] AddedColumns { get; set; }
}

/// <summary>
/// An abstract predicate. A Predicate is a logical statement, a bit like a function
/// call, that provides a pattern against which data is matched
/// </summary>
public abstract record Predicate
{
    public abstract int Arity { get; }

    public abstract Type FactType { get; }
    public abstract Symbol Name { get; }

    public abstract ITable<TFact> Evaluate<TFact>(IDb db) 
        where TFact : IFact;
    
    public abstract IObservable<FactDelta<TFact>> Observe<TFact>(IConnection conn)
        where TFact : IFact;
    
    public abstract IEnumerable<LVar> LVars { get; }
    
    public Annotations? Annotations { get; set; }

    public void Annotate(HashSet<LVar> env)
    {
        var enter = env.ToArray();
        env.UnionWith(LVars);
        var exit = env.ToArray();
        
        Annotations = new Annotations
        {
            EnvironmentEnter = enter,
            EnvironmentExit = exit,
            LVars = LVars.ToArray(),
            JoinColumns = enter.Intersect(LVars).ToArray(),
            AddedColumns = exit.Except(enter).ToArray()
        };
    }

    public abstract IOp ToOp();
}

public abstract record Predicate<T1, T2> : Predicate 
    where T1 : notnull 
    where T2 : notnull
{
    public override int Arity => 2;
    public override Symbol Name { get; }

    public override IEnumerable<LVar> LVars
    {
        get
        {
            if (Item1.IsLVar)
                yield return Item1.LVar;
            if (Item2.IsLVar)
                yield return Item2.LVar;
        }
    }

    public Predicate(Symbol name, Term<T1> t1, Term<T2> t2)
    {
        Name = name;
        Item1 = t1;
        Item2 = t2;
    }

    public Term<T1> Item1 { get; set; }
    public Term<T2> Item2 { get; set; }

    public override IOp ToOp()
    {
        return new EvaluatePredicate<Fact<T1, T2>> { Predicate = this };
    }
}

public abstract record Predicate<T1, T2, T3> : Predicate 
    where T1 : notnull
    where T2 : notnull 
    where T3 : notnull
{
    public override int Arity => 3;
    public override Symbol Name { get; }
    
    public override IEnumerable<LVar> LVars
    {
        get
        {
            if (Item1.IsLVar)
                yield return Item1.LVar;
            if (Item2.IsLVar)
                yield return Item2.LVar;
            if (Item3.IsLVar)
                yield return Item3.LVar;
        }
    }

    public override ITable<TFact> Evaluate<TFact>(IDb db)
    {
        return (Item1.IsValue, Item2.IsValue, Item3.IsValue) switch
        {
            (false, true, false) => Evaluate<TFact>(db, Item1.LVar, Item2.Value, Item3.LVar),
            _ => throw new Exception($"Invalid state: ({Item1.IsValue}, {Item2.IsValue}, {Item3.IsValue})")
        };
    }
    
    protected virtual ITable<TFact> Evaluate<TFact>(IDb db, LVar<T1> item1LVar, T2 item2Value, LVar<T3> item3LVar) 
        where TFact : IFact
    {
        throw new NotSupportedException($"Pattern of (LVar, Value, LVar) is not supported on this predicate: {Name.Name}");
    }

    
    public override IObservable<FactDelta<TFact>> Observe<TFact>(IConnection conn)
    {
        return (Item1.IsValue, Item2.IsValue, Item3.IsValue) switch
        {
            (false, true, false) => Evaluate<TFact>(conn, Item1.LVar, Item2.Value, Item3.LVar),
            _ => throw new Exception($"Invalid state: ({Item1.IsValue}, {Item2.IsValue}, {Item3.IsValue})")
        };
    }
    
    protected virtual IObservable<FactDelta<TFact>> Evaluate<TFact>(IConnection conn, LVar<T1> item1LVar, T2 item2Value, LVar<T3> item3LVar)
        where TFact : IFact
    {
        throw new NotSupportedException($"Pattern of (LVar, Value, LVar) is not supported on this predicate: {Name.Name}");
    }

    public Predicate(Symbol name, Term<T1> t1, Term<T2> t2, Term<T3> t3)
    {
        Name = name;
        Item1 = t1;
        Item2 = t2;
        Item3 = t3;
    }

    public Term<T1> Item1 { get; set; }
    public Term<T2> Item2 { get; set; }
    public Term<T3> Item3 { get; set; }
    
    public override string ToString()
    {
        return $"{Name.Name}({Item1}, {Item2}, {Item3})";
    }

    public override IOp ToOp()
    {
        return (Item1.IsValue, Item2.IsValue, Item3.IsValue) switch
        {
            (false, true, false) => new EvaluatePredicate<Fact<T1, T3>> { Predicate = this },
            _ => throw new Exception($"Invalid state: ({Item1.IsValue}, {Item2.IsValue}, {Item3.IsValue})")
        };
    }
}