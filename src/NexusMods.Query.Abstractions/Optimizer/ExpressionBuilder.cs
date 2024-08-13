using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using NexusMods.Query.Abstractions.Engines.Abstract;

namespace NexusMods.Query.Abstractions.Optimizer;

public class ExpressionBuilder
{
    public ExpressionBuilder()
    {
    }
    
    public Expression Build(RootQuery query)
    {
        var parameters = query.Inputs.ToDictionary(i => i, i => Expression.Parameter(i.Type, i.Name));
        var innerVariables = query.InnerVariables.ToDictionary(i => i, i => Expression.Variable(i.Type, i.Name));
        
        var combinedVariables = parameters.Concat(innerVariables).ToDictionary(kv => kv.Key, kv => (Expression)kv.Value);
        
        var outputParam = Expression.Parameter(query.Output.Type, query.Output.Name);
        var resultParam = Expression.Parameter(typeof(List<>).MakeGenericType(query.Output.Type), query.Output.Name);

        var innerExpr = Expression.Call(resultParam, "Add", null, [outputParam]);
        
        var body = query.Goal.Emit(combinedVariables, innerExpr);


        return null!;

    }
}
