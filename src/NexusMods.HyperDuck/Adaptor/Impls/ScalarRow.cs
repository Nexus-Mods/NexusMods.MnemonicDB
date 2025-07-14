using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace NexusMods.HyperDuck.Adaptor.Impls;

public class ScalarRow : IConverter
{
    public int? CanConvert(BuilderContext context)
    {
        if (context.Types.Length != 1 || context.Mode != BuilderContext.ContextMode.Row)
            return null;
        return 0;
    }



    public List<Expression> ConvertExpr(BuilderContext ctx)
    {
        var clrType = Helpers.ScalarMapping(ctx.Types[0].TypeId);
        if (clrType != ctx.ClrType)
            throw new NotImplementedException();
        
        var vector = Expression.Variable(typeof(ReadOnlyVector), "scalarVector");
        var vectorData = Expression.Variable(typeof(ReadOnlySpan<>).MakeGenericType(clrType), "scalarVectorData");
        ctx.Variables.AddRange([vector, vectorData]);
        
        ctx.ChunkPrefix.Add(Expression.Assign(vector, Expression.Call(ctx.ChunkExpr, "GetVector", [], Expression.Constant(0UL, typeof(ulong)))));
        ctx.ChunkPrefix.Add(Expression.Assign(vectorData, Expression.Call(vector, "GetData", [clrType])));
        
        return [Expression.Call(null, typeof(Helpers).GetMethod(nameof(Helpers.GetFromSpan))!.MakeGenericMethod(clrType), vectorData, ctx.RowIdxExpr)];
    }
}