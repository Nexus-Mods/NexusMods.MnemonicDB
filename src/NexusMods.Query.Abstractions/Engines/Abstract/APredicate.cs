using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace NexusMods.Query.Abstractions.Engines.Abstract;

public abstract class APredicate<TA, TB, TC, TD> : IPredicate<TA, TB, TC, TD>
{
    public Expression Emit(BindingType[] bindingTypes, Dictionary<IVariable, Expression> variables, IArgument[] args, Expression innerExpr)
    {
        var aExpr = ResolveArgument<TA>(variables, args[0]);
        var bExpr = ResolveArgument<TB>(variables, args[1]);
        var cExpr = ResolveArgument<TC>(variables, args[2]);
        var dExpr = ResolveArgument<TD>(variables, args[3]);
        
        var bindingTuple = (bindingTypes[0], bindingTypes[1], bindingTypes[2], bindingTypes[3]);
        
        switch (bindingTuple)
        {
            case (BindingType.Constant, BindingType.Variable, BindingType.Constant, BindingType.Output):
                return EmitCVCO(aExpr, (ParameterExpression)bExpr, cExpr, dExpr, innerExpr);
            default:
                throw new InvalidOperationException("Unknown binding type");
            
        }
        
        //return Emit(aExpr, bExpr, cExpr, dExpr, innerExpr);
        return null!;
    }

    protected virtual Expression EmitCVCO(Expression aExpr, ParameterExpression bExpr, Expression cExpr, Expression dExpr, Expression innerExpr)
    {
        throw new NotSupportedException($"This predicate {this} does not support the given binding type: CVCO");
    }

    //protected abstract Expression Emit(BindingType[] bindingTypes, Expression a, Expression b, Expression c, Expression d, Expression innerExpression);

    private static Expression ResolveArgument<T>(Dictionary<IVariable, Expression> variables, IArgument arg)
    {
        Expression aExpr;
        if (arg.TryGetVariable(out var foundA))
            aExpr = variables[foundA];
        else if (arg.TryGetConstant<T>(out var constantA))
            aExpr = Expression.Constant(constantA, typeof(T));
        else 
            throw new InvalidOperationException("Unknown argument type");
        return aExpr;
    }
}
