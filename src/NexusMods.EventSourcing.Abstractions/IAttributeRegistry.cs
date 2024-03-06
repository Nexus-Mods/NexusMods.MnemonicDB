using System;

namespace NexusMods.EventSourcing.Abstractions;

public interface IAttributeRegistry
{
    public void Append<TAttribute, TValue>(IAppendableChunk chunk, EntityId e, TValue value, TxId t, DatomFlags f)
        where TAttribute : IAttribute<TValue>;


    public int CompareValues<T>(T datomsValues, AttributeId attributeId, int a, int b) where T : IBlobColumn;

    public int CompareValues(AttributeId id, ReadOnlySpan<byte> a, ReadOnlySpan<byte> b);
}
