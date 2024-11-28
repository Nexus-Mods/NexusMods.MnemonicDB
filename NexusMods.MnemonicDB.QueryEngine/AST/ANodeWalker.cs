namespace NexusMods.MnemonicDB.QueryEngine.AST;

public abstract class ANodeWalker
{
    /// <summary>
    /// Called on every node when descending the tree, return null to remove the node.
    /// </summary>
    public virtual Node? OnEnter(Node node)
    {
        return node;
    }

    /// <summary>
    /// Called on every node when ascending the tree, return null to remove the node.
    /// </summary>
    public virtual Node? OnExit(Node node)
    {
        return node;
    }
}
