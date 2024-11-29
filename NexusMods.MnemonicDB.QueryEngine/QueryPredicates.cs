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
    
    public static void Declare<T>(out LVar<T> val, [CallerArgumentExpression(nameof(val))] string name = "")
    {
        val = NamedLVar<T>(name);
    }
    
    public static void Declare<T>(out LVar<T> val1, out LVar<T> val2, 
        [CallerArgumentExpression(nameof(val1))] string name1 = "", 
        [CallerArgumentExpression(nameof(val2))] string name2 = "")
    {
        val1 = NamedLVar<T>(name1);
        val2 = NamedLVar<T>(name2);
    }
    
    public static void Declare<T>(out LVar<T> val1, out LVar<T> val2, out LVar<T> val3, 
        [CallerArgumentExpression(nameof(val1))] string name1 = "", 
        [CallerArgumentExpression(nameof(val2))] string name2 = "", 
        [CallerArgumentExpression(nameof(val3))] string name3 = "")
    {
        val1 = NamedLVar<T>(name1);
        val2 = NamedLVar<T>(name2);
        val3 = NamedLVar<T>(name3);
    }
    
    public static void Declare<T>(out LVar<T> val1, out LVar<T> val2, out LVar<T> val3, out LVar<T> val4, 
        [CallerArgumentExpression(nameof(val1))] string name1 = "", 
        [CallerArgumentExpression(nameof(val2))] string name2 = "", 
        [CallerArgumentExpression(nameof(val3))] string name3 = "", 
        [CallerArgumentExpression(nameof(val4))] string name4 = "")
    {
        val1 = NamedLVar<T>(name1);
        val2 = NamedLVar<T>(name2);
        val3 = NamedLVar<T>(name3);
        val4 = NamedLVar<T>(name4);
    }
    
    public static void Declare<T>(out LVar<T> val1, out LVar<T> val2, out LVar<T> val3, out LVar<T> val4, out LVar<T> val5, 
        [CallerArgumentExpression(nameof(val1))] string name1 = "", 
        [CallerArgumentExpression(nameof(val2))] string name2 = "", 
        [CallerArgumentExpression(nameof(val3))] string name3 = "", 
        [CallerArgumentExpression(nameof(val4))] string name4 = "", 
        [CallerArgumentExpression(nameof(val5))] string name5 = "")
    {
        val1 = NamedLVar<T>(name1);
        val2 = NamedLVar<T>(name2);
        val3 = NamedLVar<T>(name3);
        val4 = NamedLVar<T>(name4);
        val5 = NamedLVar<T>(name5);
    }
    
    public static void Declare<T>(out LVar<T> val1, out LVar<T> val2, out LVar<T> val3, out LVar<T> val4, out LVar<T> val5, out LVar<T> val6, 
        [CallerArgumentExpression(nameof(val1))] string name1 = "", 
        [CallerArgumentExpression(nameof(val2))] string name2 = "", 
        [CallerArgumentExpression(nameof(val3))] string name3 = "", 
        [CallerArgumentExpression(nameof(val4))] string name4 = "", 
        [CallerArgumentExpression(nameof(val5))] string name5 = "", 
        [CallerArgumentExpression(nameof(val6))] string name6 = "")
    {
        val1 = NamedLVar<T>(name1);
        val2 = NamedLVar<T>(name2);
        val3 = NamedLVar<T>(name3);
        val4 = NamedLVar<T>(name4);
        val5 = NamedLVar<T>(name5);
        val6 = NamedLVar<T>(name6);
    }
    
}
