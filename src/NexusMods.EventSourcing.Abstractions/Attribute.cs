using System;

namespace NexusMods.EventSourcing.Abstractions;

public class Attribute<T>(UInt128 id, string name) : IAttribute<T>
{
    public void Emit<TTransaction>(EntityId entityId, T value, TTransaction tx) where TTransaction : ITransaction
    {
        throw new NotImplementedException();
    }
}
