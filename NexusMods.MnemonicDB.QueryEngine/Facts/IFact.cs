using System;
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
}
