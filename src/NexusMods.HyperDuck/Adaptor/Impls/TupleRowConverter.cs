using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace NexusMods.HyperDuck.Adaptor.Impls;

public class TupleRowConverter : IConverter
{
    public int? CanConvert(BuilderContext context)
    {
        if (context.Mode == BuilderContext.ContextMode.Row && context.ClrType.IsAssignableTo(typeof(ITuple)))
            return 0;
        return null;
    }

    public List<Expression> ConvertExpr(BuilderContext ctx)
    {
        var genericTypes = ctx.ClrType.GetGenericArguments();

        List<Expression> srcs = [];
        List<Expression> blk = [];
        
        for (var i = 0; i < ctx.Types.Length; i++)
        {
            var clrResultType = genericTypes[i];
            var nativeType = Helpers.ScalarMapping(ctx.Types[i]);
            var vector = Expression.Variable(typeof(ReadOnlyVector), $"columnVector{i}");
            var vectorData = Expression.Variable(typeof(ReadOnlySpan<>).MakeGenericType(nativeType), $"columnVectorData{i}");
            var val = Expression.Variable(nativeType, $"columnValue{i}");
            ctx.Variables.AddRange([vector, vectorData, val]);
            
            ctx.ChunkPrefix.Add(Expression.Assign(vector,
                Expression.Call(ctx.ChunkExpr, "GetVector", [], Expression.Constant((ulong)i, typeof(ulong)))));
            ctx.ChunkPrefix.Add(Expression.Assign(vectorData, Expression.Call(vector, "GetData", [nativeType])));
            
            blk.Add(Expression.Assign(val, Expression.Call(null, typeof(Helpers).GetMethod(nameof(Helpers.GetFromSpan))!.MakeGenericMethod(nativeType), 
                vectorData, ctx.RowIdxExpr)));

            if (nativeType != clrResultType)
            {
                var ctx2 = ctx with
                {
                    Types = ctx.Types.Slice(i, 1),
                    ClrType = clrResultType, 
                    Mode = BuilderContext.ContextMode.Value, 
                    CurrentValueExpr = val,
                    CurrentVectorExpr = vector,
                };
                var valueConverter = ctx.Registry.GetConverter(ctx2);
                var subblk = valueConverter.ConvertExpr(ctx2);
                blk.AddRange(subblk[..^1]);
                srcs.Add(subblk[^1]);
            }
            else
            {
                srcs.Add(val);
            }
        }

        blk.Add(Expression.New(ctx.ClrType.GetConstructors().First(), srcs));
        return blk;
    }
}
