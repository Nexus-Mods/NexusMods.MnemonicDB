using System;
using NexusMods.MnemonicDB.QueryEngine.Ops;

namespace NexusMods.MnemonicDB.QueryEngine.AST;

public record SelectNode : Node
{
    public LVar[] SelectVars { get; init; } = [];
    public override IOp ToOp()
    {
        var childNodeType = Children[0].ExitFact;
        var selectNodeType = typeof(Select<,>).MakeGenericType(childNodeType, ExitFact);
        var instance = (IOp)Activator.CreateInstance(selectNodeType, Children[0].ToOp(), this)!;
        return instance;
    }
}
