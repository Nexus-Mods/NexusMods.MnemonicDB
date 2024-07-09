using NexusMods.MnemonicDB.Abstractions.Models;
using NexusMods.MnemonicDB.QueryableParser.AST;
using NexusMods.MnemonicDB.QueryableParser.Tests.TestHelpers;

namespace NexusMods.MnemonicDB.QueryableParser.Tests;

public class AParserTest
{
    public async Task VerifyNode(INode node)
    {
        await Verify(PrettyPrinter.Print(node));
    }

    public async Task VerifyNode<TModel>(IQueryable<TModel> query)
    where TModel : IReadOnlyModel
    {
        var casted = query as Queryable<TModel>;
        casted.Should().NotBeNull();

        var ast = casted!.ToAST();
        await VerifyNode(ast);
    }
}
