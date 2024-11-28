using NexusMods.MnemonicDB.QueryEngine.Ops;

namespace NexusMods.MnemonicDB.QueryEngine.AST;

public record PredicateNode : Node
{
    /// <summary>
    /// The predicate to be evaluated
    /// </summary>
    public Predicate Predicate { get; init; } = default!;

    public override IOp ToOp()
    {
        return new EvaluatePredicate
        {
            Predicate = Predicate
        };
    }
}
