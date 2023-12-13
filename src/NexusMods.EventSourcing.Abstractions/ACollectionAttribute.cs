using System;
using System.Collections.Generic;

namespace NexusMods.EventSourcing.Abstractions;

public class ACollectionAttribute<TOwner, TType>(string name) : IAttribute
where TOwner : IEntity
{
    public bool IsScalar => false;
    public Type Owner => typeof(TOwner);
    public string Name => name;
    public IAccumulator CreateAccumulator()
    {
        return new Accumulator();
    }

    protected class Accumulator : IAccumulator
    {
        private HashSet<TType> _values = new();
        public void Add(object value)
        {
            _values.Add((TType) value);
        }

        public void Retract(object value)
        {
            _values.Remove((TType) value);
        }

        public object Get()
        {
            return _values;
        }
    }

    public Type Type => typeof(TType);
}
