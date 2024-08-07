using System.Collections.Generic;
using System.Linq;

namespace NexusMods.Query.Abstractions.Engines.TopDownLazy;

public class Engine
{
    public Engine(IEnumerable<IPredicate> predicates)
    {
        HashSet<ILVar> lvars = [ConstantLVars.Db];
        foreach (var predicate in predicates)
        {
            predicate.RegisterLVars(lvars);
        }

        var lookups = lvars
            .Select((lvar, idx) => new KeyValuePair<ILVar, int>(lvar, idx))
            .ToDictionary();
        
    }
}
