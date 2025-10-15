namespace NexusMods.MnemonicDB.Abstractions.Traits;

public interface IDatomLikeRO : IKeyPrefixLikeRO
{
    /// <summary>
    /// Gets the value of the datom as an object.
    /// </summary>
    public object ValueObject { get; }
}

public interface IDatomLikeRO<out TValue> : IDatomLikeRO 
    where TValue : notnull
{
    /// <summary>
    /// Gets the value of the datom as a typed object.
    /// </summary>
    public TValue Value { get; }
}
