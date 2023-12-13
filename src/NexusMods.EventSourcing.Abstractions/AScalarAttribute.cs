using System;

namespace NexusMods.EventSourcing.Abstractions;

/// <summary>
/// A scalar attribute that can be exposed on an entity.
/// </summary>
public abstract class AScalarAttribute<TOwner, TType>(string attrName) : IAttribute
{
    /// <inheritdoc />
    public bool IsScalar => false;

    /// <inheritdoc />
    public Type Owner => typeof(TOwner);

    /// <inheritdoc />
    public string Name => attrName;

    /// <inheritdoc />
    public IAccumulator CreateAccumulator()
    {
        return new Accumulator<TType>();
    }

    private class Accumulator<TVal> : IAccumulator
    {
        private TVal _value = default! ;
        public void Add(object value)
        {
            _value = (TVal) value;
        }

        public object Get()
        {
            return _value!;
        }
    }

    /// <summary>
    /// The data type of the attribute.
    /// </summary>
    public Type AttributeType => typeof(TType);
}
