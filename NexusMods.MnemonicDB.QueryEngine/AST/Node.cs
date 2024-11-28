using System;
using System.Collections.Generic;
using DynamicData;
using NexusMods.MnemonicDB.QueryEngine.AST.OptimizationPasses;
using NexusMods.MnemonicDB.QueryEngine.Ops;

namespace NexusMods.MnemonicDB.QueryEngine.AST;

public abstract record Node
{
    /// <summary>
    /// The LVars (and their order) that this node will return
    /// </summary>
    public LVar[] EnvironmentExit { get; init; } = [];

    public Type ExitFact { get; init; } = typeof(object);
    
    /// <summary>
    /// The child nodes of this node
    /// </summary>
    public Node[] Children { get; init; } = [];

    /// <summary>
    /// Converts the node (and its children) to a query operation
    /// </summary>
    public abstract IOp ToOp();

    public Node? Walk(ANodeWalker walker)
    {
        return Walk(walker, this); 
    }

    private static readonly ANodeWalker[] _optimizationPasses =
    [
        new GatherLVars(),
        new GatherFactTypes()
    ];
    
    public Node Optimize()
    {
        var node = this;
        foreach (var pass in _optimizationPasses)
        {
            node = Walk(pass, node) ?? throw new System.Exception("Optimization pass returned null");
        }
        return node!;
    }

    private static Node? Walk(ANodeWalker walker, Node node)
    {
        var newNode = walker.OnEnter(node);
        if (newNode == null)
            return null;
        
        var newChildren = new List<Node>();
        
        var hasChanged = false;
        foreach (var child in newNode.Children)
        {
            var newChild = Walk(walker, child);
            if (ReferenceEquals(newChild, child))
            {
                newChildren.Add(child);
                continue;
            }
            hasChanged = true;
            if (newChild != null)
                newChildren.Add(newChild);
        }

        if (hasChanged)
        {
            newNode = newNode with { Children = newChildren.ToArray() };
        }
        
        return walker.OnExit(newNode);
    }
}
