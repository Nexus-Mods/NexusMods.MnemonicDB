using JetBrains.Annotations;
using NexusMods.MnemonicDB.Abstractions.ValueSerializers;
using NexusMods.Paths;

namespace NexusMods.MnemonicDB.Abstractions.Attributes;

/// <summary>
/// An attribute that holds an <see cref="Size"/>.
/// </summary>
[PublicAPI]
public sealed class SizeAttribute(string ns, string name) : ScalarAttribute<Size, ulong, UInt64Serializer>(ns, name)
{
    /// <inheritdoc/>
    public override ulong ToLowLevel(Size value) => value.Value;

    /// <inheritdoc/>
    public override Size FromLowLevel(ulong value, AttributeResolver resolver) => Size.From(value);
}
