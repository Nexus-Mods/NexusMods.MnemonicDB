using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.QueryEngine.AST;
using NexusMods.MnemonicDB.QueryEngine.Facts;

namespace NexusMods.MnemonicDB.QueryEngine.Ops;

public class HashJoin<TLeftFact, TRightFact, TResultFact> : IOp
    where TLeftFact : IFact where TRightFact : IFact where TResultFact : IFact
{
    private readonly Func<TLeftFact,int> _leftHasher;
    private readonly Func<TRightFact,int> _rightHasher;
    private readonly Func<TLeftFact,TRightFact,bool> _equals;
    private readonly Func<TLeftFact,TRightFact,TResultFact> _merge;
    private readonly LVar[] _exitLVars;

    public HashJoin(IOp left, IOp right, JoinNode ast)
    {
        Left = left;
        Right = right;
        _exitLVars = ast.EnvironmentExit;

        var leftAst = ast.Children[0];
        var rightAst = ast.Children[1];
        
        _leftHasher = IFact.GetHasher<TLeftFact>(ast.LeftIndices);
        _rightHasher = IFact.GetHasher<TRightFact>(ast.RightIndices);
        
        _equals = IFact.GetEqual<TLeftFact, TRightFact>(ast.LeftIndices, ast.RightIndices);
        _merge = IFact.GetMerge<TLeftFact, TRightFact, TResultFact>(ast.EnvironmentExit, leftAst.EnvironmentExit, rightAst.EnvironmentExit);
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
        return new ListTable<TResultFact>(_exitLVars, results);
    }
}
