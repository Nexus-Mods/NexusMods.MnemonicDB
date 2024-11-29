using System.Collections.Generic;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.QueryEngine.Facts;
using NexusMods.MnemonicDB.QueryEngine.Ops;

namespace NexusMods.MnemonicDB.QueryEngine;

public abstract class ARule
{
    public string Name { get; init; } = "";
}

public record RuleVariant<TFact> where TFact : IFact
{
    /// <summary>
    /// The variant's operation
    /// </summary>
    public IOp Op { get; init; } = default!;
}

public class Rule<TFact> : ARule 
    where TFact : IFact
{
    /// <summary>
    /// Variants are overloads of the rule. Think of these as `Or` conditions of the rule,
    /// multiple variants are run in parallel (logically) and then their results are unioned.
    /// </summary>
    public List<RuleVariant<TFact>> Variants { get; init; } = [];
}
