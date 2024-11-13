using JetBrains.Annotations;
using NexusMods.MnemonicDB.Abstractions.ValueSerializers;

namespace NexusMods.MnemonicDB.Abstractions.Attributes;

/// <summary>
/// UInt64 attribute (ulong)
/// </summary>
[PublicAPI]
public sealed class ULongAttribute(string ns, string name) : ScalarAttribute<ulong, ulong, UInt64Serializer>(ns, name)
{
    /// <inheritdoc />
    protected override ulong ToLowLevel(ulong value) => value;

    /// <inheritdoc />
    protected override ulong FromLowLevel(ulong value, AttributeResolver resolver) => value;
}
