using NexusMods.MnemonicDB.Abstractions.ElementComparers;

namespace NexusMods.MnemonicDB.Abstractions.Attributes;

/// <summary>
/// Used to mark the cardinality of an attribute in the database
/// </summary>
public class CardinalityAttribute(string ns, string name) : ScalarAttribute<Cardinality, byte>(ValueTags.UInt8, ns, name)
{
    /// <inheritdoc />
    protected override byte ToLowLevel(Cardinality value)
    {
        return (byte)value;
    }

    /// <inheritdoc />
    protected override Cardinality FromLowLevel(byte value, ValueTags tags, AttributeResolver resolver)
    {
        return (Cardinality)value;
    }
}
