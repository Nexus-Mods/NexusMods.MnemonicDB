using System;
using System.Linq.Expressions;

namespace NexusMods.HyperDuck.Adaptor;

public interface IAssociative
{
    public bool CanAdapt(ReadOnlySpan<LogicalType> types, ReadOnlySpan<string> names, Type toType, out int priority);
    
    public Expression CreateNew(ReadOnlySpan<LogicalType> types, ReadOnlySpan<string> names, ReadOnlySpan<Expression> values, Type toType);
    Type[] GetElementTypes(Type rowType);
}