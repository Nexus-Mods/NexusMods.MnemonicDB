using System;
using System.Collections.Generic;
using System.Linq;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.QueryEngine.Tables;

namespace NexusMods.MnemonicDB.QueryEngine;

using Envs = HashSet<Dictionary<LVar, object>>;

/// <summary>
/// An abstract predicate. A Predicate is a logical statement, a bit like a function
/// call, that provides a pattern against which data is matched
/// </summary>
public abstract record Predicate
{
    public abstract int Arity { get; }

    public abstract Symbol Name { get; }

    public abstract ITable Evaluate(IDb db, ITable results);
}

public record Predicate<T1, T2> : Predicate
{
    public override int Arity => 2;
    public override Symbol Name { get; }
    public override ITable Evaluate(IDb db, ITable results)
    {
        throw new NotImplementedException();
    }

    public Predicate(Symbol name, Term<T1> t1, Term<T2> t2)
    {
        Name = name;
        Item1 = t1;
        Item2 = t2;
    }

    public Term<T1> Item1 { get; set; }
    public Term<T2> Item2 { get; set; }
}

public record Predicate<T1, T2, T3> : Predicate
{
    public override int Arity => 3;
    public override Symbol Name { get; }
    
    public override ITable Evaluate(IDb db, ITable enviroment)
    {
        switch (Item1.IsValue, Item2.IsValue, Item3.IsValue)
        {
            case (false, true, false):
                var subData = Evaluate(db, Item1.LVar, Item2.Value, Item3.LVar);
                if (enviroment.Columns.Length == 0)
                    return subData;
                enviroment = ((IMaterializedTable)enviroment).HashJoin(subData);
                break;
            default:
                throw new NotImplementedException($"No dispatch for ({Item1.IsValue}, {Item2.IsValue}, {Item3.IsValue})");
        }
        return enviroment;
    }

    protected virtual ITable Evaluate(IDb db, LVar<T1> item1LVar, T2 item2Value, LVar<T3> item3LVar)
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
}
