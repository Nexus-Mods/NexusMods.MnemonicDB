namespace NexusMods.MnemonicDB.QueryEngine;

public class ValueBox<T> : IValueBox
{
    public T Value { get; set; } = default!;
}
