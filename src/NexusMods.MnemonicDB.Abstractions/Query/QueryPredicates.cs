using System;
using System.Collections.Generic;
using NexusMods.MnemonicDB.Abstractions.Query.Predicates;

namespace NexusMods.MnemonicDB.Abstractions.Query;

/// <summary>
/// A class that contains constructors for common predicates, allowing this
/// class to be easily imported into a namespace. Provides a DSL of sorts for
/// making queries.
/// </summary>
public static class QueryPredicates
{
    /// <summary>
    /// Unpivots a collection of values into a single lvar, essentially flattening the collection
    /// </summary>
    public static Predicate Unpivot<T>(Term<IEnumerable<T>> src, Term<T> dest) => new Unpivot<T>(src, dest);
    
    public static Predicate Unpivot<T>(LVar<IEnumerable<T>> src, out LVar<T> dest)
    {
        dest = LVar.Create<T>();
        return new Unpivot<T>(src, dest);
    }
    
    public static Predicate ProjectTuple<T1, T2>(LVar<T1> a, LVar<T2> b, out LVar<(T1, T2)> o)
    {
        o = LVar.Create<(T1, T2)>();
        return new ProjectTuple<T1, T2>(a, b, o);
    }
    
    public static Predicate Project<T1, T2, TRet>(LVar<T1> lvar1, LVar<T2> lvar2, Func<T1, T2, TRet> func, out  LVar<TRet> lvarOut)
    {
        lvarOut = LVar.Create<TRet>();
        throw new NotImplementedException();
    }

    public static dynamic Environment() => new TrackingDynamicObject();

    public static void With<T>(out LVar<T> val)
    {
        val = LVar.Create<T>();
    }
    
    public static void With<T>(out LVar<T> val1, out LVar<T> val2)
    {
        val1 = LVar.Create<T>();
        val2 = LVar.Create<T>();
    }
}
