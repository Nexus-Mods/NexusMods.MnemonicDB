using System.Linq.Expressions;

namespace NexusMods.EventSourcing.Abstractions.Serialization;


/// <summary>
/// A typed value serializer for a dynamic size value.
/// </summary>
/// <typeparam name="T"></typeparam>
public interface IDynamicValueSerializer<T> : IValueSerializer
{
    /// <summary>
    /// Given an expression of type Span byte deserialize the value and return an expression of type T. The span
    /// will be the size originally returned by GetSizeExpression, when the value was serialized.
    /// </summary>
    /// <param name="span"></param>
    /// <returns></returns>
    public Expression<T> CreateDeserializationExpression(Expression span);

    /// <summary>
    /// Serialize the value into the given span and return the expression for the code, GetSizeExpression will be
    /// called first to get the size of the serialized value, and make sure the span is large enough.
    /// </summary>
    /// <param name="span"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public Expression CreateSerializationExpression(Expression span, Expression<T> value);

    /// <summary>
    /// Since the size is dynamic, this gets the expression for the size of the serialized value.
    /// </summary>
    /// <returns></returns>
    public Expression GetSizeExpression(Expression<T> value);
}
