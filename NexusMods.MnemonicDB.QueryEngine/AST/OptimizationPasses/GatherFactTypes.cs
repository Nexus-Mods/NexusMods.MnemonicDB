using NexusMods.MnemonicDB.QueryEngine.Facts;

namespace NexusMods.MnemonicDB.QueryEngine.AST.OptimizationPasses;

public class GatherFactTypes : ANodeWalker
{
    public override Node? OnExit(Node node)
    {
        return node with { ExitFact = IFact.GetFactType(node.EnvironmentExit) };
    }
}
