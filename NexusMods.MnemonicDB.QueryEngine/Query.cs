using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using DynamicData.Kernel;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;
using NexusMods.MnemonicDB.QueryEngine.Facts;
using NexusMods.MnemonicDB.QueryEngine.Ops;
using NexusMods.MnemonicDB.QueryEngine.Predicates;

namespace NexusMods.MnemonicDB.QueryEngine;

public abstract class AQuery<T> : IEnumerable<Predicate>
{
    protected readonly List<Predicate> _predicates = [];

    /// <summary>
    /// Adds a predicate to the query
    /// </summary>
    public void Add(Predicate predicate)
    {
        _predicates.Add(predicate);
    }
    
    public void Add<TAttribute, TValue>(Term<EntityId> e, TAttribute a, LVar<TValue> v)
        where TAttribute : IWritableAttribute<TValue>, IReadableAttribute<TValue>
        where TValue : notnull
    {
        Add(new Datoms<TAttribute, TValue>(e, a, v));
    }
    
    public void Add<TOther>(Term<EntityId> e, ReferenceAttribute<TOther> a, LVar<EntityId> v) 
        where TOther : IModelDefinition
    {
        Add(new Datoms<ReferenceAttribute<TOther>, EntityId>(e, a, v));
    }
    
    public IEnumerator<Predicate> GetEnumerator()
    {
        return _predicates.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
    
    /// <summary>
    /// Generate a LVar with a name based on an input from the caller
    /// </summary>
    protected static LVar<TRet> NamedLVar<TRet>(string name)
    {
        var idx = name.IndexOf(" ", StringComparison.Ordinal);
        if (idx != -1)
        {
            name = name[(idx + 1)..];
        }
        return LVar.Create<TRet>(name);
    }
}

public class Query<T1, T2> : AQuery<Query<T1, T2>> where T2 : notnull where T1 : notnull
{
    private readonly LVar<T1> _lvar1;
    private readonly LVar<T2> _lvar2;

    public Query(out LVar<T1> lvar1,  out LVar<T2> lvar2, 
        [CallerArgumentExpression(nameof(lvar1))] string name1 = "", 
        [CallerArgumentExpression(nameof(lvar2))] string name2 = "")
    {
        _lvar1 = lvar1 = NamedLVar<T1>(name1);
        _lvar2 = lvar2 = NamedLVar<T2>(name2);
    }
    
    public Func<IDb, IEnumerable<(T1, T2)>> TableFn()
    {
        AnnotatePredicates();

        IOp acc = new EvaluatePredicate
        {
            Predicate = _predicates.First()
        };
        foreach (var predicate in _predicates.Skip(1))
        {
            acc = new HashJoin
            {
                Left = acc,
                Right = new EvaluatePredicate
                {
                    Predicate = predicate
                }
            }.Prepare();
        }

        return db =>
        {
            var results = acc.Execute(db);
            throw new NotImplementedException();
        };
    }

    private void AnnotatePredicates()
    {
        var env = new HashSet<LVar>();
        foreach (var predicate in _predicates)
        {
            predicate.Annotate(env);
        }
        
    }
}

public abstract class CompiledQuery
{
    protected Dictionary<int, Predicate[]> _compiledQueries = new();
    protected int CalculateMask(params bool[] values)
    {
        var mask = 0;
        for (var i = 0; i < values.Length; i++)
        {
            if (values[i])
            {
                mask |= 1 << i;
            }
        }

        return mask;
    }
    
}

public class CompiledQuery<T1, T2, TRet> : CompiledQuery 
    where T1 : notnull where T2 : notnull
{
    private readonly List<Predicate> _predicates;
    private readonly LVar<TRet> _retVar;
    private readonly LVar<T1> _lvar1;
    private readonly LVar<T2> _lvar2;

    public CompiledQuery(LVar<T1> lvar1, LVar<T2> lvar2, LVar<TRet> retVar, List<Predicate> predicates)
    {
        _lvar1 = lvar1;
        _lvar2 = lvar2;
        _predicates = predicates;
        _retVar = retVar;
    }
}
