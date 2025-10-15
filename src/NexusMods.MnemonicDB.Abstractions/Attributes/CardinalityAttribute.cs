using JetBrains.Annotations;
using NexusMods.MnemonicDB.Abstractions.ValueSerializers;

namespace NexusMods.MnemonicDB.Abstractions.Attributes;

/// <summary>
/// Used to mark the cardinality of an attribute in the database
/// </summary>
[PublicAPI]
public sealed class CardinalityAttribute(string ns, string name) : ScalarAttribute<Cardinality, byte, UInt8Serializer>(ns, name)
{
    /// <inheritdoc />
    public override byte ToLowLevel(Cardinality value) => (byte)value;

    /// <inheritdoc />
    public override Cardinality FromLowLevel(byte value, AttributeResolver resolver) => (Cardinality)value;
}
