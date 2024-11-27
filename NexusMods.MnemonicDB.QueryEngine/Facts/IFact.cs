using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Linq.Expressions;

namespace NexusMods.MnemonicDB.QueryEngine.Facts;

public interface IFact
{
    public int Arity { get; }

    public static readonly Type?[] TupleTypes =
    [
        null!,
        typeof(Fact<>),
        typeof(Fact<,>),
        typeof(Fact<,,>),
        typeof(Fact<,,,>),
    ];

    /// <summary>
    /// Returns the hash code of the value at the given index
    /// </summary>
    public int GetHashCode(int idx);
    
    /// <summary>
    /// Creates a function that hashes the fact based on the given item indexes
    /// </summary>
    public static Func<T, int> GetHasher<T>(params int[] indexes) 
        where T : IFact
    {
        var combineMethod = typeof(HashCode).GetMethod("Combine", [typeof(int), typeof(int)])!;
        
        var input = Expression.Parameter(typeof(T), "input");
        var startHash = Expression.Call(input, "GetHashCode", [], Expression.Constant(indexes[0]));
        
        foreach (var idx in indexes.Skip(1))
        {
            startHash = Expression.Call(combineMethod, startHash, Expression.Call(input, "GetHashCode", [typeof(int)], Expression.Constant(idx)));
        }
        
        return Expression.Lambda<Func<T, int>>(startHash, input).Compile();
    }
    
    public static Func<TLeft, TRight, bool> GetEqual<TLeft, TRight>(int[] leftIdxes, int[] rightIdxes)
    where TLeft : IFact
    where TRight : IFact
    {
        
        var leftIntput = Expression.Parameter(typeof(TLeft), "left");
        var rightIntput = Expression.Parameter(typeof(TRight), "right");
        
        List<Expression> equalsExprs = [];

        for (var idx = 0; idx < leftIdxes.Length; idx++)
        {
            equalsExprs.Add(Expression.Equal(Expression.Property(leftIntput, "Item" + leftIdxes[idx]), 
                Expression.Property(rightIntput, "Item" + rightIdxes[idx])));
        }

        var fullExpr = equalsExprs.Aggregate(Expression.And);
        
        return Expression.Lambda<Func<TLeft, TRight, bool>>(fullExpr, leftIntput, rightIntput).Compile();
    }

    public static Func<TLeftFact, TRightFact, TResultFact> GetMerge<TLeftFact, TRightFact, TResultFact>(LVar[] resultLVars, LVar[] leftLVars, LVar[] rightLVars) 
        where TLeftFact : IFact 
        where TRightFact : IFact 
        where TResultFact : IFact
    {
        List<Expression> selectExprs = [];
        var leftIntput = Expression.Parameter(typeof(TLeftFact), "left");
        var rightIntput = Expression.Parameter(typeof(TRightFact), "right");
        foreach (var lvar in resultLVars)
        {
            var leftIdx = Array.IndexOf(leftLVars, lvar);
            if (leftIdx != -1)
            {
                selectExprs.Add(Expression.Property(leftIntput, "Item" + leftIdx));
            }
            else
            {
                var rightIdx = Array.IndexOf(rightLVars, lvar);
                selectExprs.Add(Expression.Property(rightIntput, "Item" + rightIdx));
            }
        }
        var resultExpr = Expression.New(typeof(TResultFact).GetConstructors()[0], selectExprs);
        
        return Expression.Lambda<Func<TLeftFact, TRightFact, TResultFact>>(resultExpr, leftIntput, rightIntput).Compile();
    }

    public static Func<TPrevFact, TResultFact> GetSelector<TPrevFact, TResultFact>(LVar[] prevLVars, LVar[] selectLVars)
    {
        List<Expression> selectExprs = [];
        var prevIntput = Expression.Parameter(typeof(TPrevFact), "prev");
        foreach (var lvar in selectLVars)
        {
            var idx = Array.IndexOf(prevLVars, lvar);
            selectExprs.Add(Expression.Property(prevIntput, "Item" + idx));
        }
        var resultExpr = Expression.New(typeof(TResultFact).GetConstructors()[0], selectExprs);
        
        return Expression.Lambda<Func<TPrevFact, TResultFact>>(resultExpr, prevIntput).Compile();
    }
}
