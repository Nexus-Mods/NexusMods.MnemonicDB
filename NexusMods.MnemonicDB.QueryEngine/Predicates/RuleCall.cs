using System;
using System.Collections.Generic;
using System.Linq;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.QueryEngine.Facts;
using NexusMods.MnemonicDB.QueryEngine.Ops;

namespace NexusMods.MnemonicDB.QueryEngine.Predicates;

public record RuleCall<T1, T2> : Predicate 
    where T1 : notnull 
    where T2 : notnull
{
    private readonly LVar[] _lvars;

    public RuleCall(Rule<Fact<T1, T2>> rule, LVar<T1> t1, LVar<T2> t2)
    {
        Rule = rule;
        _lvars = [t1, t2];
    }
    
    public override int Arity => 2;
    public override Type FactType => typeof(Fact<T1, T2>);
    
    public override Symbol Name { get; } = Symbol.Intern("RuleCall");
    public override ITable<TFact> Evaluate<TFact>(IDb db)
    {
        if (db.DbCache.TryGetValue(Rule, out var cached))
            return (ITable<TFact>)cached;
        var results = new List<Fact<T1, T2>>();
        foreach (var variant in Rule.Variants)
        {
            var result = ((IOp<Fact<T1, T2>>)variant.Op).Execute(db);
            results.AddRange(result);
        }
        var resultTable = new ListTable<Fact<T1, T2>>(_lvars, results);
        db.DbCache.TryAdd(Rule, resultTable);
        return (ITable<TFact>)resultTable;
    }

    public override IObservable<FactDelta<TFact>> Observe<TFact>(IConnection conn)
    {
        throw new NotImplementedException();
    }


    public Rule<Fact<T1, T2>> Rule { get; }
    public override IEnumerable<LVar> LVars => _lvars;
    public override IOp ToOp()
    {
        return new EvaluatePredicate<Fact<T1, T2>> { Predicate = this };
    }
}
