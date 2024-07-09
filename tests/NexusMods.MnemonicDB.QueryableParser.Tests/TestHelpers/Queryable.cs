using System.Collections;
using System.Linq.Expressions;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.QueryableParser.AST;
using NexusMods.MnemonicDB.TestModel;

namespace NexusMods.MnemonicDB.QueryableParser.Tests.TestHelpers;

public class Queryable<TElement> : IQueryable<TElement>
{
    public Queryable(Expression expression)
    {
        Expression = expression;
    }
    public IEnumerator<TElement> GetEnumerator()
    {
        throw new NotImplementedException();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    private static Parser _parser = new Parser(new[] { typeof(Loadout).GetMethod("Query")! });
    public INode ToAST()
    {
        return _parser.Parse(Expression);
    }

    public Type ElementType => typeof(TElement);
    
    public Expression Expression { get; }
    public IQueryProvider Provider => new LoadoutQueryProvider();
}
