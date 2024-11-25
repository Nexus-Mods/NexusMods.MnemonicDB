using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;
using NexusMods.MnemonicDB.QueryEngine.Predicates;

namespace NexusMods.MnemonicDB.QueryEngine;

/// <summary>
/// A class that contains constructors for common predicates, allowing this
/// class to be easily imported into a namespace. Provides a DSL of sorts for
/// making queries.
/// </summary>
public static class QueryPredicates
{
    /// <summary>
    /// Generate a LVar with a name based on an input from the caller
    /// </summary>
    private static LVar<TRet> NamedLVar<TRet>(string name)
    {
        var idx = name.IndexOf(" ", StringComparison.Ordinal);
        if (idx != -1)
        {
            name = name[(idx + 1)..];
        }
        return LVar.Create<TRet>(name);
    }

    public static Predicate Project<T1, T2, TRet>(LVar<T1> lvar1, LVar<T2> lvar2, Func<T1, T2, TRet> func, out  LVar<TRet> lvarOut)
    {
        lvarOut = LVar.Create<TRet>();
        throw new NotImplementedException();
    }

    public static Predicate Db<TAttr, TValue>(LVar<EntityId> e, TAttr attr, TValue tval)
        where TAttr : IWritableAttribute<TValue>, IReadableAttribute<TValue>
        where TValue : notnull
    {
        throw new NotImplementedException();
    }

    public static Predicate Db<TAttr, TValue>(LVar<EntityId> e, TAttr attr, LVar<TValue> tval)
        where TAttr : IWritableAttribute<TValue>, IReadableAttribute<TValue>
        where TValue : notnull
    {
        return new Datoms<TAttr, TValue>(e, attr, tval);
    }

    
    /*
    public static Predicate Db(LVar<EntityId> e, ReferenceAttribute a, EntityId v)
    {
        throw new NotImplementedException();
    }*/


    public static Predicate Db<TOtherModel>(LVar<EntityId> e, BackReferenceAttribute<TOtherModel> backref,
        out LVar<EntityId> referer, [CallerMemberName] string name = "") 
        where TOtherModel : IModelDefinition
    {
        referer = NamedLVar<EntityId>(name);
        throw new NotImplementedException();
    }
    
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
