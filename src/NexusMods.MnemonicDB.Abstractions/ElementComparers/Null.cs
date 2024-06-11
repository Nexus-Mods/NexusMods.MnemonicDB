namespace NexusMods.MnemonicDB.Abstractions.ElementComparers;

/// <summary>
/// A null value, used to represent a null as a value. This is mostly used for
/// marker attributes that don't have a value.
/// </summary>
public struct Null
{
    /// <summary>
    /// A singleton instance of the null value.
    /// </summary>
    public static Null Instance { get; } = new();

    /// <inheritdoc />
    public override string ToString()
    {
        return "Null";
    }
}
