using System.Collections.Generic;
using System.Runtime.CompilerServices;
using NexusMods.MnemonicDB.Query.Clauses;

namespace NexusMods.MnemonicDB.Query;

public class QueryBuilder
{
    private readonly List<LVar> _from = [];
    private readonly List<IClause> _clauses = [];
    private readonly List<LVar> _return = [];
    
    public QueryBuilder In<T>(out LVar lvar, [CallerArgumentExpression("lvar")] string lvarName ="")
    {
        var afterSpace = lvarName.IndexOf(' ');
        lvar = LVar.Create<T>(afterSpace == -1 ? lvarName : lvarName[afterSpace..]);
        return this;
    }
    
    public QueryBuilder AddClause(IClause clause)
    {
        _clauses.Add(clause);
        return this;
    }
    
    public QueryBuilder Declare<T>(out LVar lvar, [CallerArgumentExpression("lvar")] string lvarName = "")
    {
        var afterSpace = lvarName.IndexOf(' ');
        lvar = LVar.Create<T>(afterSpace == -1 ? lvarName : lvarName[afterSpace..]);
        return this;
    }


    public QueryBuilder Find(params LVar[] lvars)
    {
        _return.Clear();
        _return.AddRange(lvars);
        return this;
    }
}
