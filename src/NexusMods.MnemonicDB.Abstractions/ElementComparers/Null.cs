namespace NexusMods.MnemonicDB.Abstractions.ElementComparers;

/// <summary>
/// A null value, used to represent a null as a value. This is mostly used for
/// marker attributes that don't have a value.
/// </summary>
public readonly struct Null
{
    private readonly int _dummy; // This is to ensure that the struct is not default

    private Null(int dummy)
    {
        _dummy = dummy;
    }
    
    /// <summary>
    /// A singleton instance of the null value.
    /// </summary>
    public static Null Instance { get; } = new(1);
    
    /// <summary>
    /// True if this is a default value.
    /// </summary>
    public bool IsDefault => _dummy == 0;

    /// <inheritdoc />
    public override string ToString()
    {
        return "Null";
    }
}
