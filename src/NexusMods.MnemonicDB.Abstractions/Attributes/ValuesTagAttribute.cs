using JetBrains.Annotations;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.ValueSerializers;

namespace NexusMods.MnemonicDB.Abstractions.Attributes;

/// <summary>
/// An attribute that represents a value tag value
/// </summary>
[PublicAPI]
public sealed class ValuesTagAttribute(string ns, string name) : ScalarAttribute<ValueTag, byte, UInt8Serializer>(ns, name)
{
    /// <inheritdoc />
    public override byte ToLowLevel(ValueTag value) => (byte)value;

    /// <inheritdoc />
    public override ValueTag FromLowLevel(byte value, AttributeResolver resolver) => (ValueTag)value;
}
