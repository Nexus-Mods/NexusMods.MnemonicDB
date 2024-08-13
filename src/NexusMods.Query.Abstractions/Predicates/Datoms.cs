using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.DatomIterators;
using NexusMods.Query.Abstractions.Engines.Abstract;

namespace NexusMods.Query.Abstractions.Predicates;

public class Datoms<THighLevel, TLowLevel> : APredicate<IDb, EntityId, Attribute<THighLevel, TLowLevel>, THighLevel> 
{
    protected override Expression EmitCVCO(Expression aExpr, ParameterExpression bExpr, Expression cExpr, Expression dExpr, Expression innerExpr)
    {

        return Foreach(Expression.Call(aExpr, "Datoms", [typeof(THighLevel), typeof(TLowLevel)], [cExpr, dExpr]), datom =>
        {
            return Expression.Block(
                Expression.Assign(bExpr, Expression.Property(datom, "E")),
                innerExpr);
        });
    }
    
    
    /// <summary>
    /// Constructs a foreach loop via Linq Expressions
    /// </summary>
    private Expression Foreach(Expression collExpr, Func<Expression, Expression> body)
    {
        var enumeratorCall = Expression.Call(collExpr, "GetEnumerator", null);
        var enumerableParam = Expression.Parameter(enumeratorCall.Type, "enumerator");
        var enumAssign = Expression.Assign(enumerableParam, enumeratorCall);
        
        var breakLabel = Expression.Label("LoopBreak");

        var loop = Expression.Loop(
            Expression.Block(
                new[] { enumerableParam },
                enumAssign,
                Expression.IfThenElse(
                    Expression.Call(enumerableParam, "MoveNext", null),
                    body(Expression.Property(enumerableParam, "Current")),
                    Expression.Break(breakLabel)
                ),
                Expression.Label(breakLabel)
            )
        );
        return loop;
    }
}
