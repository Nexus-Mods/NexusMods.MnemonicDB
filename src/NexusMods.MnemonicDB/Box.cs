namespace NexusMods.MnemonicDB;

/// <summary>
/// A object wrapper for a value, useful for mutating values between calls to a closure
/// </summary>
public sealed class Box<T>
{
    public Box(T value)
    {
        Value = value;
    }

    public T Value { get; set; }
}
