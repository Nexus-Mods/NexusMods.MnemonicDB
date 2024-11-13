using JetBrains.Annotations;
using NexusMods.MnemonicDB.Abstractions.ValueSerializers;

namespace NexusMods.MnemonicDB.Abstractions.Attributes;

/// <summary>
/// An attribute that holds an uint16 value.
/// </summary>
[PublicAPI]
public sealed class UInt16Attribute(string ns, string name) : ScalarAttribute<ushort, ushort, UInt16Serializer>(ns, name)
{
    /// <inheritdoc />
    protected override ushort ToLowLevel(ushort value) => value;

    /// <inheritdoc />
    protected override ushort FromLowLevel(ushort value, AttributeResolver resolver) => value;
}
