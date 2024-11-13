using JetBrains.Annotations;
using NexusMods.MnemonicDB.Abstractions.ValueSerializers;

namespace NexusMods.MnemonicDB.Abstractions.Attributes;

/// <summary>
/// An attribute that holds an uint64 value.
/// </summary>
[PublicAPI]
public sealed class UInt64Attribute(string ns, string name) : ScalarAttribute<ulong, ulong, UInt64Serializer>(ns, name)
{
    /// <inheritdoc />
    protected override ulong ToLowLevel(ulong value) => value;

    /// <inheritdoc />
    protected override ulong FromLowLevel(ulong value, AttributeResolver resolver) => value;
}
