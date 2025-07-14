using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace NexusMods.HyperDuck.Adaptor.Impls;

public class ListValueConverter : IConverter
{
    public int? CanConvert(BuilderContext ctx)
    {
        return ctx.Mode == BuilderContext.ContextMode.Value 
               && ctx.Types[0].TypeId == DuckDbType.List
               && ctx.ClrType.IsAssignableTo(typeof(IList))
            ? 0 : null;
    }

    public List<Expression> ConvertExpr(BuilderContext ctx)
    {
        using var childType = ctx.Types[0].ListChildType();
        var nativeType = Helpers.ScalarMapping(childType);
        var clrInnerType = ctx.ClrType.GetGenericArguments()[0];
        
        // If the types match perfectly, we can just initialize a List from a span 
        if (nativeType != clrInnerType)
        {
            throw new NotImplementedException();
        }
        
        var listVector = Expression.Variable(typeof(ReadOnlyVector), "listVector");
        ctx.Variables.Add(listVector);
        ctx.ChunkPrefix.Add(Expression.Assign(listVector, Expression.Call(ctx.CurrentVectorExpr, "GetListChild", null)));
        
        var blk = new List<Expression>();
        var listResult = Expression.Variable(ctx.ClrType, "listResult");
        ctx.Variables.Add(listResult);
        
        blk.Add(Expression.Assign(listResult, Expression.New(ctx.ClrType)));
        blk.Add(Expression.Call(typeof(CollectionExtensions).GetMethod("AddRange")!.MakeGenericMethod(nativeType), 
            listResult, Expression.Call(listVector, "Slice", [nativeType], ctx.CurrentValueExpr)));
        blk.Add(listResult);
        return blk;
    }
}
