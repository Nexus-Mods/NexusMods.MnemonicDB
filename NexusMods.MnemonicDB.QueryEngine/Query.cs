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
    private readonly List<Predicate> _predicates = [];

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
    
    public void Add<T1, T2>(Rule<Fact<T1, T2>> rule, LVar<T1> t1, LVar<T2> t2) 
        where T1 : notnull
        where T2 : notnull
    {
        Add(new RuleCall<T1, T2>(rule, t1, t2));
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
        var op = ToAST(_predicates, lvar1, lvar2)
            .Optimize()
            .ToOp();
        
        return db =>
        {
            var table = op.Execute(db);
            return ((ITable<Fact<T1, T2>>)table).Facts;
        };
    }
    
    public void AsVariant<T1, T2>(Rule<Fact<T1, T2>> rule, LVar<T1> t1, LVar<T2> t2) 
        where T1 : notnull
        where T2 : notnull
    {
        var op = ToAST(_predicates, t1, t2)
            .Optimize()
            .ToOp();
        
        rule.Variants.Add(new RuleVariant<Fact<T1, T2>>
        {
            Op = op
        });
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

