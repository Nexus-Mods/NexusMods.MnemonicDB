using System.Collections.Generic;
using System.Collections.Immutable;
using Metadata = System.Collections.Immutable.ImmutableDictionary<NexusMods.MnemonicDB.Abstractions.Symbol, object>;

namespace NexusMods.MnemonicDB.LogicEngine;


public interface IPredicate : IHasMetadata
{
    public IGoal Source { get; }
    public static IPredicate Create<TName>(params object[] args) where TName : IGoal, new() 
        => new Predicate<TName>(new TName(), args, Metadata.Empty);
}

public record Predicate<T>(T Name, object[] Args, Metadata Metadata) : IPredicate
    where T : IGoal, new()
{
    public override string ToString() => $"{Name}/{Args.Length}({string.Join(", ", Args)})";
    
    public IGoal Source => Name;
    
    /// <summary>
    /// True if the argument at the given index is a constant, false if it is a variable bound or unbound.
    /// </summary>
    public bool IsConstant(int idx) => Args[idx] is not LVar;
    
    /// <summary>
    /// True if the argument at the given index bound or a constant, false if it is unbound.
    /// </summary>
    public bool IsBound(int idx, ImmutableHashSet<LVar> bound) => IsConstant(idx) || (Args[idx] is LVar lvar && bound.Contains(lvar));
    
    /// <summary>
    /// Gets the argument at the given index as a constant
    /// </summary>
    /// <param name="idx"></param>
    public object this[int idx] => Args[idx];

    /// <summary>
    /// Return a copy of this predicate with the given arguments.
    /// </summary>
    public IPredicate WithArgs(List<object> newChildren)
    {
        return new Predicate<T>(Name, newChildren.ToArray(), Metadata);
    }

    /// <summary>
    /// Return a copy of this predicate with the given name and the same arguments
    /// and metadata.
    /// </summary>
    public IPredicate WithName<TNew>()
    where TNew : IGoal, new()
    {
        return new Predicate<TNew>(new TNew(), Args, Metadata);
    }
}
