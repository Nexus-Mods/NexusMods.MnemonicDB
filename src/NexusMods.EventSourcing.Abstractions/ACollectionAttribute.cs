using System;

namespace NexusMods.EventSourcing.Abstractions;

public class ACollectionAttribute<TOwner, TType>(string name) : IAttribute
where TOwner : IEntity
{
    public bool IsScalar => false;
    public Type Owner => typeof(TOwner);
    public string Name => name;
    public IAccumulator CreateAccumulator()
    {
        throw new NotImplementedException();
    }

    public Type Type => typeof(TType);
}
