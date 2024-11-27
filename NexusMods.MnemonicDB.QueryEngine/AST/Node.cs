using NexusMods.MnemonicDB.QueryEngine.Ops;

namespace NexusMods.MnemonicDB.QueryEngine.AST;

public abstract record Node
{
    /// <summary>
    /// The LVars (and their order) that this node will return
    /// </summary>
    public LVar[] EnvironmentExit { get; init; } = [];
    
    /// <summary>
    /// The child nodes of this node
    /// </summary>
    public Node[] Children { get; init; } = [];

    /// <summary>
    /// Converts the node (and its children) to a query operation
    /// </summary>
    public abstract IOp ToOp();
}
