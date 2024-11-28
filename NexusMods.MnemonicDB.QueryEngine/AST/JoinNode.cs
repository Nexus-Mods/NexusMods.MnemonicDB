using System;
using NexusMods.MnemonicDB.QueryEngine.Ops;

namespace NexusMods.MnemonicDB.QueryEngine.AST;

/// <summary>
/// Represents a join operation between two child nodes
/// </summary>
public record JoinNode : Node
{
    /// <summary>
    /// The LVars used to join the results of the child nodes
    /// </summary>
    public LVar[] JoinLVars { get; init; } = [];
    
    /// <summary>
    /// The values from the left child that will be copied into the result
    /// </summary>
    public LVar[] CopyLVars { get; init; } = [];
    
    /// <summary>
    /// The values from the right child that will be copied into the result
    /// </summary>
    public LVar[] NewLVars { get; init; } = [];
    
    /// <summary>
    /// LVar join indices for the left child
    /// </summary>
    public int[] LeftIndices { get; init; } = [];
    
    /// <summary>
    /// LVar join indices for the right child
    /// </summary>
    public int[] RightIndices { get; init; } = [];
    
    public override IOp ToOp()
    {
        var leftFact = Children[0].ExitFact;
        var rightFact = Children[1].ExitFact;
        var hashJoinOpType = typeof(HashJoin<,,>).MakeGenericType(leftFact, rightFact, ExitFact);
        var leftOp = Children[0].ToOp();
        var rightOp = Children[1].ToOp();
        var hashJoinOpInstance = (IOp)Activator.CreateInstance(hashJoinOpType, leftOp, rightOp, this)!;
        return hashJoinOpInstance;
    }
} 
