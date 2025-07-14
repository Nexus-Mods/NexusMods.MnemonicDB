using System;
using System.Linq.Expressions;

namespace NexusMods.HyperDuck.Adaptor;

public interface ISequential
{
    public bool CanAdapt(Type type, out int priority);
    
    /// <summary>
    /// Called when the adapter needs to "open" the collection for editing. Length may be null
    /// if the final length is unknown. 
    /// </summary>
    public Expression OpenExpression(Expression coll, Expression? length = null);

    public Expression Create(Type concreteType, Expression? length = null);
    public Expression SetElementExpr(Expression coll, Expression index, Expression value);
    public Expression GetElementExpr(Expression coll, Expression index);
    public Expression? CloseExpression(Expression coll);
    
    /// <summary>
    /// Get the element type this collection holds
    /// </summary>
    public Type GetElementType(Type collType);
    
}