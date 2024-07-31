using NexusMods.MnemonicDB.Abstractions.ElementComparers;

namespace NexusMods.MnemonicDB.Abstractions.Attributes;

/// <summary>
/// An attribute that represents a value tag value
/// </summary>
public class ValuesTagAttribute(string ns, string name) : ScalarAttribute<ValueTags, byte>(ValueTags.UInt8, ns, name)
{
    /// <inheritdoc />
    protected override byte ToLowLevel(ValueTags value) => (byte)value;

    /// <inheritdoc />
    protected override ValueTags FromLowLevel(byte lowLevelType, ValueTags tags, RegistryId registryId) 
        => (ValueTags)lowLevelType;
}
