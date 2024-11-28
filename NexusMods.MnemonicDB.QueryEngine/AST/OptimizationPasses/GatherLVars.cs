using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace NexusMods.MnemonicDB.QueryEngine.AST.OptimizationPasses;

/// <summary>
/// An optimizer pass that annotates all the nodes with LVars needed at each node
/// </summary>
public class GatherLVars : ANodeWalker
{
    public override Node? OnExit(Node node)
    {
        switch (node)
        {
            case PredicateNode p:
                return p with { EnvironmentExit = p.Predicate.LVars.ToArray() };
            case JoinNode j:
                Debug.Assert(j.Children.Length == 2);
                var a = j.Children[0].EnvironmentExit;
                var b = j.Children[1].EnvironmentExit;
                var joinLVars = a.Intersect(b).ToArray();
                var copyLVars = a.Except(b).ToArray();
                var newLVars = b.Except(a).ToArray();
                return j with
                {
                    JoinLVars = joinLVars,
                    LeftIndices = joinLVars.Select(lv => Array.IndexOf(a, lv)).ToArray(),
                    RightIndices = joinLVars.Select(lv => Array.IndexOf(b, lv)).ToArray(),
                    CopyLVars = copyLVars,
                    NewLVars = newLVars,
                    EnvironmentExit = joinLVars.Concat(copyLVars).Concat(newLVars).ToArray()
                };
            case SelectNode s:
                return s with { EnvironmentExit = s.SelectVars };
            default:
                throw new NotSupportedException($"Node type {node.GetType()} is not supported by GatherLVars");
        }
    }
}
