using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.ValueSerializers;

namespace NexusMods.MnemonicDB.Abstractions.Attributes;

/// <summary>
/// Used to mark the cardinality of an attribute in the database
/// </summary>
public sealed class CardinalityAttribute(string ns, string name) : ScalarAttribute<Cardinality, byte, UInt8Serializer>(ns, name)
{
    /// <inheritdoc />
    protected override byte ToLowLevel(Cardinality value) => (byte)value;

    /// <inheritdoc />
    protected override Cardinality FromLowLevel(byte value, AttributeResolver resolver) => (Cardinality)value;
}
