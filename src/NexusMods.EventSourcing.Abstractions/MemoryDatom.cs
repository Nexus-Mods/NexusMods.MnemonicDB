namespace NexusMods.EventSourcing.Abstractions;

/// <summary>
/// Untyped memory datom, this is used to get raw access to a linear memory representation of several
/// datoms.
///
/// DEPRECATED: This is should probably be replaced by chunks and a more structured memory layout.
/// </summary>
/// <typeparam name="T"></typeparam>
public unsafe struct MemoryDatom<T>
where T : IBlobColumn
{
    public EntityId* EntityIds;
    public AttributeId* AttributeIds;
    public TxId* TransactionIds;
    public T Values;
}
