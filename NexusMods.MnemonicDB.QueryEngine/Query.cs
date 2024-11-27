using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using DynamicData.Kernel;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;
using NexusMods.MnemonicDB.QueryEngine.AST;
using NexusMods.MnemonicDB.QueryEngine.Facts;
using NexusMods.MnemonicDB.QueryEngine.Ops;
using NexusMods.MnemonicDB.QueryEngine.Predicates;

namespace NexusMods.MnemonicDB.QueryEngine;

public class Query : IEnumerable<Predicate>
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
    
    public Func<IDb, IEnumerable<Fact<T1, T2>>> AsTableFn<T1, T2>(LVar<T1> lvar1, LVar<T2> lvar2) 
        where T1 : notnull
        where T2 : notnull
    {
        var ast = ToAST(_predicates, [lvar1, lvar2]);
        AnnotatePredicates();

        IOp acc = new EvaluatePredicate
        {
            Predicate = _predicates.First()
        };
        
        foreach (var predicate in _predicates.Skip(1))
        {
            acc = HashJoin.Create(acc, new EvaluatePredicate
            {
                Predicate = predicate
            });
        }

        acc = Op.Select(acc, [lvar1, lvar2]);

        return db =>
        {
            var table = acc.Execute(db);
            return ((ITable<Fact<T1, T2>>)table).Facts;
        };
    }

    /// <summary>
    /// Converts a list of predicates into an unoptimized AST
    /// </summary>
    private static Node ToAST(List<Predicate> predicates, params LVar[] lvars)
    {
        Node node = new PredicateNode
        { 
            Predicate = predicates[0]
        };
        
        foreach (var nextp in predicates.Skip(1))
        {
            node = new JoinNode
            {
                Children = [node, new PredicateNode
                    {
                        Predicate = nextp
                    }],
            };
        }

        return new SelectNode
        {
            SelectVars = lvars,
            Children = [node]
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

