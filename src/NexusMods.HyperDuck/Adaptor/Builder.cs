using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace NexusMods.HyperDuck.Adaptor;

public class Builder
{
    public delegate T ConverterFn<out T>(ref Result result);
    
    private readonly Registry _registry;

    public Builder(Registry registry)
    {
        _registry = registry;
    }
    
    public Func<Result, T> Build<T>(ReadOnlySpan<LogicalType> columnTypes, ReadOnlySpan<string> columnNames)
    {
        var ctx = BuilderContext.CreateNew(columnTypes, typeof(T), _registry);
        var resultAdapter = _registry.GetConverter(ctx);
        var resultExprs = resultAdapter.ConvertExpr(ctx);

        
        List<Expression> blk = [];
        blk.AddRange(ctx.FunctionPrefix);
        // Chunk fetch loop
        blk.Add(Expression.Label(ctx.NextChunkLabel));
        blk.Add(Expression.Assign(ctx.RowIdxExpr, Expression.Constant(0UL, typeof(ulong))));
        blk.Add(Expression.Assign(ctx.ChunkExpr, Expression.Call(ctx.ResultExpr, "FetchChunk", null)));
        // Jump to the end if the current block isn't valid
        blk.Add(Expression.IfThen(Expression.IsFalse(Expression.Property(ctx.ChunkExpr, "IsValid")), Expression.Goto(ctx.LastChunkLabel)));
        blk.Add(Expression.Assign(ctx.ChunkSizeExpr, Expression.Property(ctx.ChunkExpr, "Size")));;
        blk.AddRange(ctx.ChunkPrefix);
        // Emit the body
        blk.AddRange(resultExprs);
        // Emit the ending of the body
        blk.AddRange(ctx.ChunkPostfix);
        blk.Add(Expression.Call(ctx.ChunkExpr, "Dispose", null));;
        blk.Add(Expression.Goto(ctx.NextChunkLabel));
        blk.Add(Expression.Label(ctx.LastChunkLabel));
        blk.AddRange(ctx.FunctionPostfix);
        blk.Add(ctx.ReturnExpr);

        var body = Expression.Block(ctx.Variables, blk);
        return Expression.Lambda<Func<Result, T>>(body, ctx.ResultExpr).Compile();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    internal static T GetFromSpan<T>(ReadOnlySpan<T> span, int index)
    {
        return span[index];
    }

    private Type GetElementType(LogicalType columnType)
    {
        return columnType.TypeId switch
        {
            DuckDbType.Integer => typeof(int),
            DuckDbType.BigInt => typeof(long),
            DuckDbType.Varchar => typeof(StringElement),
            DuckDbType.List => typeof(ListEntry),
            _ => throw new NotImplementedException()
        };
    }

    public Func<Result, T> Build<T>(Result result)
    {
        Span<LogicalType> types = stackalloc LogicalType[(int)result.ColumnCount];
        List<string> names = new List<string>((int)result.ColumnCount);
        for (var i = 0; i < types.Length; i++)
        {
            var col = result.GetColumnInfo((ulong)i);
            types[i] = col.GetLogicalType();
            names.Add(col.Name);
        }
        
        var c = Build<T>(types, CollectionsMarshal.AsSpan(names));
        for (var i = 0; i < types.Length; i++)
        {
            types[i].Dispose();
        }

        return c;
    }
}