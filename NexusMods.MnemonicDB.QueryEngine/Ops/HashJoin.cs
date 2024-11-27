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
        
        var klass = typeof(HashJoin<,,>).MakeGenericType(leftOp.FactType, rightOp.FactType, factOutput);

        return (IOp)Activator.CreateInstance(klass, leftOp, rightOp)!;
    }
}

public class HashJoin<TLeftFact, TRightFact, TResultFact> : IOp
    where TLeftFact : IFact where TRightFact : IFact where TResultFact : IFact
{
    private readonly Func<TLeftFact,int> _leftHasher;
    private readonly Func<TRightFact,int> _rightHasher;
    private readonly Func<TLeftFact,TRightFact,bool> _equals;
    private readonly Func<TLeftFact,TRightFact,TResultFact> _merge;

    public HashJoin(IOp left, IOp right)
    {
        Left = left;
        Right = right;
        
        var joinLVars = left.LVars.Intersect(right.LVars).ToArray();
        var copyLVars = left.LVars.Except(joinLVars).ToArray();
        var newLVars = right.LVars.Except(joinLVars).ToArray();
        LVars = joinLVars.Concat(copyLVars).Concat(newLVars).ToArray();

        var leftIdxes = joinLVars.Select(lv => Array.IndexOf(left.LVars, lv)).ToArray();
        _leftHasher = IFact.GetHasher<TLeftFact>(leftIdxes);
        
        var rightIdxes = joinLVars.Select(lv => Array.IndexOf(right.LVars, lv)).ToArray();
        _rightHasher = IFact.GetHasher<TRightFact>(rightIdxes);
        
        _equals = IFact.GetEqual<TLeftFact, TRightFact>(leftIdxes, rightIdxes);
        _merge = IFact.GetMerge<TLeftFact, TRightFact, TResultFact>(LVars, left.LVars, right.LVars);
    }

    public required IOp Left { get; init; }
    public required IOp Right { get; init; }
    
    public ITable Execute(IDb db)
    {
        var leftHash = new Dictionary<int, LinkedList<TLeftFact>>();
        
        foreach (var fact in ((ITable<TLeftFact>)Left.Execute(db)).Facts)
        {
            var hash = _leftHasher(fact);
            if (!leftHash.ContainsKey(hash))
            {
                leftHash[hash] = [];
            }
            leftHash[hash].AddFirst(fact);
        }
        
        
        List<TResultFact> results = [];
        foreach (var fact in ((ITable<TRightFact>)Right.Execute(db)).Facts)
        {
            var hash = _rightHasher(fact);
            if (!leftHash.TryGetValue(hash, out var lefts))
            {
                continue;
            }
            
            foreach (var leftFact in lefts)
            {
                if (_equals(leftFact, fact))
                {
                    results.Add(_merge(leftFact, fact));
                }
            }
        }
        return new ListTable<TResultFact>(LVars, results);
    }

    public LVar[] LVars { get; }
    public Type FactType => typeof(TResultFact);


}
