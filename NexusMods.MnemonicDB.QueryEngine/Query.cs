using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using DynamicData.Kernel;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.QueryEngine.Predicates;
using NexusMods.MnemonicDB.QueryEngine.Tables;

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
    
    public void Add<T1, T2>((LVar<T1> a, LVar<T2> b) tuple, out LVar<(T1, T2)> o)
    {
        o = LVar.Create<(T1, T2)>();
        Add(new ProjectTuple<T1, T2>(tuple.a, tuple.b, o));
    }
    
    public void Add<TAttribute, TValue>(Term<EntityId> e, TAttribute a, Term<TValue> v)
    where TAttribute : IWritableAttribute<TValue> 
    where TValue : notnull
    {
        Add(new Datoms<TAttribute, TValue>(e, a, v));
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
    
    /// <summary>
    /// Execute the query
    /// </summary>
    public CompiledQuery<T1, T2, TRet> Return<TRet>(LVar<TRet> ret)
    {
        return new CompiledQuery<T1, T2, TRet>(_lvar1, _lvar2, ret, _predicates);
    }

    public CompiledQuery<T1, T2, (T1, T2)> Compile()
    {
        var outVar = LVar.Create<(T1, T2)>();
        Add(new ProjectTuple<T1, T2>(_lvar1, _lvar2, outVar));
        return new CompiledQuery<T1, T2, (T1, T2)>(_lvar1, _lvar2, outVar, _predicates);
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
    
    protected Predicate[] Optimize(List<LVar> startingEnv, LVar exit, List<Predicate> predicates)
    {
        List<Predicate> optimized = new();
        var existing = startingEnv.ToImmutableArray();
        
        foreach (var predicate in predicates)
        {
            var bound = predicate.Bind(existing);
            optimized.Add(bound);
            existing = bound.EnvironmentExit;
        }

        var required = new HashSet<LVar> { exit };
        
        for (int i = optimized.Count - 1 ; i >= 0; i--)
        {
            optimized[i] = optimized[i].Clean(required);
        }

        return optimized.ToArray();
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
    
    public IEnumerable<TRet> Run(Optional<T1> t1 = default, Optional<T2> t2 = default)
    {
        var mask = CalculateMask(t1.HasValue, t2.HasValue);
        if (!_compiledQueries.TryGetValue(mask, out var compiledQuery))
        {
            var startingEnv = new List<LVar>();
            if (t1.HasValue)
            {
                startingEnv.Add(_lvar1);
            }
            
            if (t2.HasValue)
            {
                startingEnv.Add(_lvar2);
            }

            compiledQuery = Optimize(startingEnv, _retVar, _predicates);
            _compiledQueries.Add(mask, compiledQuery);
        }

        var startTable = new AppendableTable([_lvar1, _lvar2]);
        if (t1.HasValue && !t2.HasValue)
        {
            ((IAppendableColumn<T1>)startTable[_lvar1]).Add(t1.Value);
            startTable.FinishRow();
        }
        if (t1.HasValue && t2.HasValue)
        {
            ((IAppendableColumn<T1>)startTable[_lvar1]).Add(t1.Value);
            ((IAppendableColumn<T2>)startTable[_lvar2]).Add(t2.Value);
            startTable.FinishRow();
        }
        else throw new NotImplementedException();

        var table = startTable.Freeze();
        foreach (var predicate in compiledQuery)
        {
            table = predicate.RunFn(table);
        }

        return (IColumn<TRet>)table[0];
    }
    
}
