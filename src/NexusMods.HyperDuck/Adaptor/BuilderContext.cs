using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace NexusMods.HyperDuck.Adaptor;

public ref struct BuilderContext
{
    public required Registry Registry { get; init; }
    public List<ParameterExpression> Variables { get; init; } = [];
    public LabelTarget NextChunkLabel { get; } = Expression.Label("nextChunk");
    public LabelTarget NextRowLabel { get; } = Expression.Label("nextRow");
    
    public LabelTarget LastChunkLabel { get; } = Expression.Label("lastChunk");
    public ReadOnlySpan<LogicalType> Types { get; init; }
    
    /// <summary>
    /// The C# type the DuckDB data should be conformed to
    /// </summary>
    public required Type ClrType { get; init; }
    
    /// <summary>
    /// `Result` object being processed from DuckDb
    /// </summary>
    public required ParameterExpression ResultExpr { get; init; }
    
    public required ParameterExpression ReturnExpr { get; init; }
    public required ParameterExpression ChunkExpr { get; init; }
    
    /// <summary>
    /// Ulong that holds the current offset of the row being processed in the chunk
    /// </summary>
    public required ParameterExpression RowIdxExpr { get; set; }
    
    /// <summary>
    /// Ulong that holds the current total row count
    /// </summary>
    public required ParameterExpression TotalRowExpr { get; set; }
    
    public required Expression CurrentVectorExpr { get; set; }
    public required Expression CurrentValueExpr { get; set; }

    public List<Expression> FunctionPrefix { get; set; } = [];
    public List<Expression> ChunkPrefix { get; set; } = [];
    public List<Expression> RowPrefix { get; set; } = [];
    public List<Expression> RowPostfix { get; set; } = [];
    public List<Expression> ChunkPostfix { get; set; } = [];
    public List<Expression> FunctionPostfix { get; set; } = [];

    public ContextMode Mode { get; init; } = ContextMode.Chunk;
    
    /// <summary>
    /// The totsl size of the current chunk
    /// </summary>
    public required ParameterExpression ChunkSizeExpr { get; init; }

    public LabelTarget ChunkEndLabel { get; } = Expression.Label("chunkEnd");

    public enum ContextMode
    {
        // Current context is a chunk of DB results
        Chunk,
        // Current context is a row
        Row,
        // Single value conversion
        Value,
    }

    public BuilderContext()
    {
        
    }
    
    public static BuilderContext CreateNew(ReadOnlySpan<LogicalType> types, Type clrType, Registry registry)
    {
        var ctx = new BuilderContext
        {
            Registry = registry,
            Types = types,
            ClrType = clrType,
            ResultExpr = Expression.Parameter(typeof(Result), "results"),
            ChunkExpr = Expression.Parameter(typeof(ReadOnlyChunk), "chunk"), 
            RowIdxExpr = Expression.Parameter(typeof(ulong), "row"),
            TotalRowExpr = Expression.Parameter(typeof(ulong), "totalRowCount"),
            ReturnExpr = Expression.Parameter(clrType, "returnValue"),
            ChunkSizeExpr = Expression.Parameter(typeof(ulong), "chunkSize"),
            CurrentValueExpr = null!,
            CurrentVectorExpr = null!,
        };
        ctx.Variables.AddRange([
            ctx.ChunkExpr, ctx.RowIdxExpr, ctx.TotalRowExpr, ctx.ReturnExpr, ctx.ChunkSizeExpr
        ]);
        return ctx;
    }
}
