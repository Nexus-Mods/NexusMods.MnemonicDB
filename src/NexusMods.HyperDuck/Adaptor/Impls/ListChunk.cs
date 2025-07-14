using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace NexusMods.HyperDuck.Adaptor.Impls;

public class ListChunk : IConverter
{
    public int? CanConvert(BuilderContext context)
    {
        if (context.Mode != BuilderContext.ContextMode.Chunk || !context.ClrType.IsAssignableTo(typeof(IList)))
            return null;
        return 0;
    }

    public List<Expression> ConvertExpr(BuilderContext ctx)
    {
        var acc = Expression.Variable(ctx.ClrType, "acc");
        ctx.Variables.Add(acc);
        ctx.FunctionPrefix.Add(Expression.Assign(acc, Expression.New(ctx.ClrType)));
        ctx.FunctionPostfix.Add(Expression.Assign(ctx.ReturnExpr, acc));
        var innerCtx = ctx with
        {
            Mode = BuilderContext.ContextMode.Row,
            ClrType = ctx.ClrType.GetGenericArguments()[0]
        };
        var innerConverter = ctx.Registry.GetConverter(innerCtx);
        var innerExpr = innerConverter.ConvertExpr(innerCtx);
        
        var blk = new List<Expression>();
        ctx.ChunkPrefix.Add(Expression.Assign(ctx.RowIdxExpr, Expression.Constant(0UL, typeof(ulong))));

        blk.Add(Expression.Label(ctx.NextRowLabel));
        blk.Add(Expression.IfThen(Expression.GreaterThanOrEqual(ctx.RowIdxExpr, 
            ctx.ChunkSizeExpr), 
            Expression.Goto(ctx.ChunkEndLabel)));
        blk.AddRange(innerExpr[..^1]);
        blk.Add(Expression.Call(acc, "Add", [], innerExpr[^1]));
        blk.Add(Expression.Assign(ctx.RowIdxExpr, Expression.Add(ctx.RowIdxExpr, Expression.Constant(1UL, typeof(ulong)))));
        blk.Add(Expression.Goto(ctx.NextRowLabel));
        blk.Add(Expression.Label(ctx.ChunkEndLabel));;
        return blk;
    }
}
