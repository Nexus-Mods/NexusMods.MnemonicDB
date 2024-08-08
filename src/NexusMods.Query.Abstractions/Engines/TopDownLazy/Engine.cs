using System;
using System.Collections.Generic;
using System.Linq;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.Query.Abstractions.Engines.TopDownLazy;

public class Engine
{
    public static Func<IDb, IEnumerable<(TA, TB)>> Build<TA, TB> (IEnumerable<IPredicate> predicates, LVar<TA> a, LVar<TB> b)
    {
        HashSet<ILVar> lvars = [ConstantLVars.Db];
        foreach (var predicate in predicates)
        {
            predicate.RegisterLVars(lvars);
        }

        var lookups = lvars
            .Select((lvar, idx) => new KeyValuePair<ILVar, int>(lvar, idx))
            .ToDictionary();

        var bound = new HashSet<ILVar> { ConstantLVars.Db };

        var inOrder = lookups
            .OrderBy(v => v.Value)
            .Select(v => v.Key)
            .ToArray();

        var context = new Context
        {
            LVars = lookups,
            Bound = bound
        };
        
        var constructed = predicates
            .Select(predicate => predicate.MakeLazy(context))
            .ToArray();
        
        var aIdx = lookups[a];
        var bIdx = lookups[b];
        var dbIdx = lookups[ConstantLVars.Db];

        return db =>
        {
            var initial = GC.AllocateUninitializedArray<ILVarBox>(lvars.Count);
            for (int i = 0; i < lvars.Count; i++)
            {
                initial[i] = inOrder[i].MakeBox();
            }
            ((LVarBox<IDb>)initial[dbIdx]).Value = db;
            
            IEnumerable<ILVarBox[]> acc = [initial];
            
            acc = constructed.Aggregate(acc, static (current, lazy) => lazy(current));
            return acc.Select(boxes => (((LVarBox<TA>)boxes[aIdx]).Value, ((LVarBox<TB>)boxes[bIdx]).Value));
        };
    }
}
