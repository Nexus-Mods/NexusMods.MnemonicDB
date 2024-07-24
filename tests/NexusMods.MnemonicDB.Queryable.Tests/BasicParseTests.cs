using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Queryable.QueryableParser;

namespace NexusMods.MnemonicDB.Queryable.Tests;

public class BasicParseTests
{
    
    [Fact]
    public void CanParseWhereClause()
    {
        var rule = Query.Build
            .In(x, a, b)
            .Datoms(x, a, Op.LessThan, b)
            .Find();

    }
}
