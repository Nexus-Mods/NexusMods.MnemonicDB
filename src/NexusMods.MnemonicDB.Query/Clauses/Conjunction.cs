using System.Linq;

namespace NexusMods.MnemonicDB.Query.Clauses;

public class Conjunction : IClause
{
    private readonly IClause[] _inner;
    private readonly LVar[] _references;

    public Conjunction(params IClause[] inner)
    {
        _inner = inner;
        References = inner.SelectMany(clause => clause.References).Distinct().ToArray();
    }

    public LVar[] References { get; }
}
