using JetBrains.Annotations;
using NexusMods.MnemonicDB.Abstractions.ValueSerializers;

namespace NexusMods.MnemonicDB.Abstractions.Attributes;

/// <summary>
/// An attribute that holds a boolean value.
/// </summary>
[PublicAPI]
public sealed class BooleanAttribute(string ns, string name) : ScalarAttribute<bool, byte, UInt8Serializer>(ns, name)
{
    /// <inheritdoc/>
    public override byte ToLowLevel(bool value) => value ? (byte)1 : (byte)0;

    /// <inheritdoc/>
    public override bool FromLowLevel(byte value, AttributeResolver resolver) => value == 1;
}
