using System;
using System.Collections.Generic;
using System.Linq;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.QueryEngine.Facts;

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
    public override ITable Evaluate(IDb db)
    {
        if (db.DbCache.TryGetValue(Rule, out var cached))
            return (ITable)cached;
        var results = new List<Fact<T1, T2>>();
        foreach (var variant in Rule.Variants)
        {
            var result = (ITable<Fact<T1, T2>>)variant.Op.Execute(db);
            results.AddRange(result.Facts);
        }
        var resultTable = new ListTable<Fact<T1, T2>>(_lvars, results);
        db.DbCache.TryAdd(Rule, resultTable);
        return resultTable;
    }

    public Rule<Fact<T1, T2>> Rule { get; }
    public override IEnumerable<LVar> LVars => _lvars;
}
