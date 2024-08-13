using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace NexusMods.Query.Abstractions.Engines.Abstract;

/// <summary>
/// Toplevel interface for all predicates
/// </summary>
public interface IPredicate
{ 
    Expression Emit(BindingType[] bindingTypes, Dictionary<IVariable,Expression> combinedVariables, IArgument[] innerExpression, Expression innerExpr);
}


/// <summary>
/// A predicate with a specific type argument
/// </summary>
public interface IPredicate<TA> : IPredicate
{
    
}

/// <summary>
/// A predicate with two specific type arguments
/// </summary>
public interface IPredicate<TA, TB> : IPredicate
{
    
}


/// <summary>
/// A predicate with three specific type arguments
/// </summary>
public interface IPredicate<TA, TB, TC> : IPredicate
{
    
}

/// <summary>
/// A predicate with four specific type arguments
/// </summary>
public interface IPredicate<TA, TB, TC, TD> : IPredicate
{
    
}


