using NexusMods.MnemonicDB.Queryable.KnowledgeDatabase;

namespace NexusMods.MnemonicDB.Queryable.AbstractSyntaxTree;

public class Rule
{
    /// <summary>
    /// The head of the rule also known as the conclusion.
    /// </summary>
    public required IPredicate Head { get; init; }
    
    /// <summary>
    /// The body of the rule also known as the premises.
    /// </summary>
    public required IPredicate Body { get; init; }
}
