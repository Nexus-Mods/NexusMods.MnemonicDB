using System.Collections.Generic;
using System.Numerics;
using MnemonicDB.Query.Engines.Datalog;
using NexusMods.MnemonicDB.Abstractions;

namespace MnemonicDB.Query.Sources;

/// <summary>
/// [Range, Max, ?current]
/// </summary>
public class Range<TA, TB> : IFactSource<TA, TB>
where TA : INumber<TA>, TB
{
    public override Symbol Predicate => Source.Range;
    
    public override IEnumerable<TEnv> Lazy<TEnv>(IEnumerable<TEnv> envs, Fact<TA, TB> fact)
    {
        foreach (var env in envs)
        {
            if (env.TryGet(fact.A, out var max))
            {
                for (var current = TA.Zero; current.CompareTo(max) < 0; current += TA.One)
                {
                    yield return env.Bind(fact.B, current);
                }
            }
        }
    }


    



    
    
}
