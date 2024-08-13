using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace NexusMods.Query.Abstractions.Engines.Abstract;

/// <summary>
/// A high-level goal definition. This combines a fact with its arguments
/// </summary>
public record Goal(IPredicate Predicate, IArgument[] Arguments) : IGoal
{
    /// <summary>
    /// Create a new goal with the given predicate and argument
    /// </summary>
    public static Goal New<TA>(IPredicate<TA> predicate, TA a) where TA : notnull =>
        new(predicate, [Argument<TA>.New(a)]);
    
    /// <summary>
    /// Create a new goal with the given predicate and arguments
    /// </summary>
    public static Goal New<TA, TB>(IPredicate<TA, TB> predicate, TA a, TB b) where TA : notnull where TB : notnull =>
        new(predicate, [Argument<TA>.New(a), Argument<TB>.New(b)]);
    
    /// <summary>
    /// Create a new goal with the given predicate and arguments
    /// </summary>
    public static Goal New<TA, TB, TC>(IPredicate<TA, TB, TC> predicate, TA a, TB b, TC c) where TA : notnull where TB : notnull where TC : notnull =>
        new(predicate, [Argument<TA>.New(a), Argument<TB>.New(b), Argument<TC>.New(c)]);
    
    /// <summary>
    /// Create a new goal with the given predicate and arguments
    /// </summary>
    public static Goal New<TA, TB, TC, TD>(IPredicate<TA, TB, TC, TD> predicate, TA a, TB b, TC c, TD d) where TA : notnull where TB : notnull where TC : notnull where TD : notnull =>
        new(predicate, [Argument<TA>.New(a), Argument<TB>.New(b), Argument<TC>.New(c), Argument<TD>.New(d)]);

    public override string ToString()
    {
        return $"{Predicate}({string.Join(", ", Arguments.Select(t => t.ToString()))})";
    }

    public Environment.Execute Emit(EnvironmentDefinition env, Environment.Execute innerExpr)
    {
        return Predicate.Emit(BindingTypes, env, Arguments, innerExpr);
    }


    public BindingType[] BindingTypes { get; set; } = [];
    
    public bool IsSupported(Span<BindingType> bindingTypes)
    {
        throw new NotImplementedException();
    }
}
