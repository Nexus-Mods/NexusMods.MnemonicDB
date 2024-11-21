using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using DynamicData;
using NexusMods.MnemonicDB.Abstractions.Query;
using NexusMods.MnemonicDB.QueryEngine.Tables;

namespace NexusMods.MnemonicDB.QueryEngine;

/// <summary>
/// An abstract predicate. A Predicate is a logical statement, a bit like a function
/// call, that provides a pattern against which data is matched
/// </summary>
public abstract record Predicate
{
    /// <summary>
    /// The terms that make up the predicate
    /// </summary>
    public abstract IEnumerable<(string Name, ITerm Term)> Terms { get; }

    public abstract IEnumerable<ImmutableDictionary<LVar, object>> Apply(IEnumerable<ImmutableDictionary<LVar, object>> envStream);
    
    public ImmutableArray<LVar> EnvironmentEnter { get; init; } = ImmutableArray<LVar>.Empty;
    public ImmutableArray<LVar> EnvironmentExit { get; init; }= ImmutableArray<LVar>.Empty;
    public ImmutableHashSet<LVar> Inputs { get; init; } = ImmutableHashSet<LVar>.Empty;
    public ImmutableHashSet<LVar> Outputs { get; init; } = ImmutableHashSet<LVar>.Empty;
    
    private Func<ITable, ITable>? _runFn = null;

    public Func<ITable, ITable> RunFn => _runFn!;
    
    public TableJoiner? Joiner { get; init; }
    
    /// <summary>
    /// The src and dest columns to copy
    /// </summary>
    public (int Src, int Dest)[] CopyColumns { get; init; } = [];
    
    /// <summary>
    /// The join columns on the output environment
    /// </summary>
    public (int Src, int Dest)[] KeyColumns { get; init; } = [];
    
    /// <summary>
    /// The emitted columns
    /// </summary>
    public int[] EmitColumns { get; init; } = [];
    
    public Predicate Bind(ImmutableArray<LVar> existing)
    {
        var lvars = Terms.Where(t => t.Term.IsLVar)
            .Select(t => t.Term.LVar)
            .ToHashSet();
        return this with
        {
            Inputs = lvars.Intersect(existing).ToImmutableHashSet(),
            Outputs = lvars.Except(existing).ToImmutableHashSet(),
            EnvironmentEnter = existing,
            EnvironmentExit = [..existing.Union(lvars)]
        };
    }

    public Predicate Clean(HashSet<LVar> required)
    {
        var lvars = Terms.Where(t => t.Term.IsLVar)
            .Select(t => t.Term.LVar)
            .ToHashSet();

        var newEnter = EnvironmentEnter
            .Where(i => Inputs.Contains(i) || required.Contains(i))
            .ToImmutableArray();
        var newExit = EnvironmentExit
            .Where(i => required.Contains(i) || Outputs.Contains(i))
            .ToImmutableArray();

        // The indexes of the columns that are net-new
        var newNewColumns = newExit.Select((v, idx) => (v, idx))
            .Where(t => !newEnter.Contains(t.v))
            .Select(e => e.idx)
            .ToArray();

        var newKeyColumns = Inputs.Select(i =>
        {
            var inputIdx = newEnter.IndexOf(i);
            var outputIdx = newExit.IndexOf(i);
            return (inputIdx, outputIdx);
        }).ToArray();

        var newCopyColumns = EnvironmentEnter
            .Where(i => newExit.Contains(i) && !Inputs.Contains(i))
            .Select(i =>
            {
                var inputIdx = newEnter.IndexOf(i);
                var outputIdx = newExit.IndexOf(i);
                return (inputIdx, outputIdx);
            }).ToArray();

        var newNode =  this with
        {
            Joiner = new TableJoiner(newEnter.ToArray(), newCopyColumns, newKeyColumns, newNewColumns, newExit.ToArray()),
            CopyColumns = newCopyColumns,
            KeyColumns = newKeyColumns,
            EmitColumns = newNewColumns,
            EnvironmentEnter = newEnter,
            EnvironmentExit = newExit,
        };
        
        newNode._runFn = newNode.MakeRunFn();
        
        foreach (var lvar in lvars)
            required.Add(lvar);
        return newNode;
    }

    private Func<ITable, ITable> MakeRunFn()
    {
        char GetTermSig(ITerm term)
        {
            if (term.IsValue)
                return 'C';
            if (Inputs.Contains(term.LVar))
                return 'I';
            return 'O';
        }
        
        var sig = "Run_" + string.Join("", Terms.Select(t => GetTermSig(t.Term)));

        var method = GetType()
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .FirstOrDefault(m => m.Name == sig);
        
        if (method == null)
            throw new Exception($"The predicate {GetType().Name} does not have a method named {sig} which it requires to execute this query");
        
        List<Expression> args = [Expression.Parameter(typeof(ITable))];

        foreach (var t in Terms)
        {
            if (t.Term.IsValue)
                args.Add(Expression.Constant(t.Term.ObjectValue));
            else if (Inputs.Contains(t.Term.LVar))
            {
                var idx = EnvironmentEnter.IndexOf(t.Term.LVar);
                args.Add(Expression.Constant(idx));
            }
            else
            {
                var idx = EnvironmentExit.IndexOf(t.Term.LVar);
                args.Add(Expression.Constant(idx));
            }
        }
        var call = Expression.Call(Expression.Constant(this), method, args);
        var lambda = Expression.Lambda<Func<ITable, ITable>>(call, args.Take(1).Cast<ParameterExpression>().ToArray());

        return lambda.Compile();
    }
}
