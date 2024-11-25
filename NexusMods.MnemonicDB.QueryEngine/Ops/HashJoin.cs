using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.QueryEngine.Facts;

namespace NexusMods.MnemonicDB.QueryEngine.Ops;

public class HashJoin
{
    public static IOp Create(IOp leftOp, IOp rightOp)
    {
        var left = leftOp.LVars;
        var right = rightOp.LVars;
        var joinColumns = left.Intersect(right).ToArray();
        var copyColumns = left.Except(joinColumns).ToArray();
        var newColumns = right.Except(joinColumns).ToArray();
        
        var newLVars = joinColumns.Concat(copyColumns).Concat(newColumns).ToArray();
        var factOutput = IFact.TupleTypes[newLVars.Length]!
            .MakeGenericType(newLVars.Select(lv => lv.Type).ToArray());
        
        var factLeft = leftOp.FactType;
        var factRight = rightOp.FactType;
        var leftJoinIdxs = joinColumns.Select(lv => Array.IndexOf(left, lv)).ToArray();
        var rightJoinIdxs = joinColumns.Select(lv => Array.IndexOf(right, lv)).ToArray();

        var hasherLeft = IFact.GetHasher<IFact>(leftJoinIdxs);
        var hasherRight = IFact.GetHasher<IFact>(rightJoinIdxs);
        
        var klass = typeof(HashJoin<,,>).MakeGenericType(factLeft, factRight, factOutput);

        return (IOp)Activator.CreateInstance(klass, leftOp, rightOp)!;
    }
}

public class HashJoin<TLeftFact, TRightFact, TResultFact>(IOp left, IOp right, 
    LVar[] resultColumns,
    Func<TLeftFact, int> leftHasher, 
    Func<TRightFact, int> rightHasher,
    Func<TLeftFact, TRightFact, bool> equalCheck,
    Func<TLeftFact, TRightFact, TResultFact> selectFn) : IOp
    where TLeftFact : IFact where TRightFact : IFact where TResultFact : IFact
{
    public required IOp Left { get; init; }
    public required IOp Right { get; init; }

    public ITable Execute(IDb db)
    {
        var leftHash = new Dictionary<int, List<TLeftFact>>();

        foreach (var fact in ((ITable<TLeftFact>)Left.Execute(db)).Facts)
        {
            var hash = leftHasher(fact);
            if (!leftHash.TryGetValue(hash, out var list))
            {
                list = [];
                leftHash[hash] = list;
            }
            list.Add(fact);
        }
        
        List<TResultFact> results = [];
        foreach (var fact in ((ITable<TRightFact>)Right.Execute(db)).Facts)
        {
            var rightHash = rightHasher(fact);
            if (!leftHash.TryGetValue(rightHash, out var leftFacts))
                continue;

            foreach (var leftFact in leftFacts)
            {
                if (!equalCheck(leftFact, fact))
                    continue;
                
                results.Add(selectFn(leftFact, fact));
            }
        }
        return new ListTable<TResultFact>(resultColumns, results);
    }

    public LVar[] LVars { get; private set; } = [];
    public Type FactType { get; private set; } = typeof(IFact);


}
