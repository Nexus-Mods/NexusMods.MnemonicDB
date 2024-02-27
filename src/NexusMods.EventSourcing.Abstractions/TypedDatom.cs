namespace NexusMods.EventSourcing.Abstractions;

public interface ITypedDatom
{

}

public struct TypedDatom<TAttribute, TValue> : ITypedDatom
    where TAttribute : IAttribute<TValue>
{
    public required EntityId E { get; init; }
    public required TValue V { get; init; }
    public required TxId T { get; init; }
    public required DatomFlags Flags { get; init; }
}
