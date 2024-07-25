using System.Collections;
using NexusMods.MnemonicDB.Queryable.AbstractSyntaxTree;
using NexusMods.MnemonicDB.Queryable.Engines.Lazy;
using NexusMods.MnemonicDB.Queryable.KnowledgeDatabase;
using NexusMods.MnemonicDB.Queryable.Predicates.StdLib;

namespace NexusMods.MnemonicDB.Queryable.Tests;

public class BasicParseTests
{
    
    [Fact]
    public void CanParseWhereClause()
    {
        var engine = new Engine(new Definitions([]));
        
        var rule = Query
            .From(out LVar<IEnumerable<int>> input)
            .Declare(out LVar<int> contain)
            .Contains(input, contain)
            .Select(contain)
            .ToLazy();
        
        rule([1, 2, 3]).Should().BeEquivalentTo([1, 2, 3]);

    }
}
