using JetBrains.Annotations;
using NexusMods.MnemonicDB.Abstractions.ValueSerializers;

namespace NexusMods.MnemonicDB.Abstractions.Attributes;

/// <summary>
/// An attribute that holds an uint32 value.
/// </summary>
[PublicAPI]
public sealed class UInt32Attribute(string ns, string name) : ScalarAttribute<uint, uint, UInt32Serializer>(ns, name)
{
    /// <inheritdoc />
    protected override uint ToLowLevel(uint value) => value;

    /// <inheritdoc />
    protected override uint FromLowLevel(uint value, AttributeResolver resolver) => value;
}
