using NexusMods.MnemonicDB.Abstractions.ElementComparers;

namespace NexusMods.MnemonicDB.Abstractions.Attributes;

/// <summary>
/// An attribute that represents a value tag value
/// </summary>
public sealed class ValuesTagAttribute(string ns, string name) : ScalarAttribute<ValueTag, byte>(ValueTag.UInt8, ns, name)
{
    /// <inheritdoc />
    protected override byte ToLowLevel(ValueTag value) => (byte)value;

    /// <inheritdoc />
    protected override ValueTag FromLowLevel(byte value, AttributeResolver resolver) => (ValueTag)value;
}
