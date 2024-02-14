using System;

namespace NexusMods.EventSourcing.Abstractions;

public interface ISeekableIterator
{
    public bool SeekLast();
    public bool SeekFirst();
    public bool Next();
    public bool Prev();

    public EntityId EntityId { get; }
    public TxId TxId { get; }
    public bool IsAssert { get; }
    public bool IsRetract { get; }
    public Type Attr { get; }


    public void SetUpper(EntityId id);
    public void SetLower(EntityId id);
    public void SetAttrs(Type Attrs);
    public void SetUpper(TxId id);
    public void SetLower(TxId id);
    public void SetUpperValue<TAttr, TVal>(TVal val)
    where TAttr : IAttribute<TVal>;

}
