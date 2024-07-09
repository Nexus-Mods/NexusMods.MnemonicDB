using NexusMods.MnemonicDB.QueryableParser.AST;
using NexusMods.MnemonicDB.QueryableParser.Tests.TestHelpers;
using NexusMods.MnemonicDB.TestModel;

namespace NexusMods.MnemonicDB.QueryableParser.Tests;

public class BasicParserTests : AParserTest
{
    [Fact]
    public async Task CanParseSingleWhereClause()
    {
        // Arrange
        var query = QueryableModels.Loadouts;

        // Act
        var filtered = query.Where(f => f.Name == "Test");

        // Assert
        await VerifyNode(filtered);
    }

    [Fact]
    public async Task DoubleWhereClause()
    {
        
        // Arrange
        var query = QueryableModels.Loadouts;

        // Act
        var filtered = query.Where(f => f.Name == "Test")
            .Where(f => f.Name == "Test2");

        // Assert
        await VerifyNode(filtered);
    }

    [Fact]
    public async Task CanParseSingleWhereClauseFromSyntax()
    {
        var query = from f in QueryableModels.Loadouts
                    where f.Name == "Test"
                    select f;
        await VerifyNode(query);
    }

    public interface IClause
    {
        
    }
    public record Clause(object E, object A, object V) : IClause
    {

        public static implicit operator Clause((int, string, int) a)
        {
            throw new NotImplementedException();
        }
        
    }

    public void DoStuff(params Clause[] clauses)
    {
        
    }

    public void Test()
    {
        Clause foo = (4, "test", 11);
        var clauses = new List<Clause>()
        {
            (4, "test", 3),
        };

        dynamic source = null!;
        var query = source.Find(out LVar loadout)
            .In(out var Source)
            .Where(
                (loadout, Loadout.Name, "Test"))
            .AsLoadout();
            

        DoStuff(
            (1, "foo", 3),
            (42, "zip", 4),
            (4, "test", 3));
    }
}
