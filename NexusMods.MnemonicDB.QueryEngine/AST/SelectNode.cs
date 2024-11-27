using NexusMods.MnemonicDB.QueryEngine.Ops;

namespace NexusMods.MnemonicDB.QueryEngine.AST;

public record SelectNode : Node
{
    public LVar[] SelectVars { get; init; } = [];
    public override IOp ToOp()
    {
        return Ops.Op.
    }
}
