using JetBrains.Annotations;
using NexusMods.MnemonicDB.Abstractions.ValueSerializers;

namespace NexusMods.MnemonicDB.Abstractions.Attributes;

/// <summary>
/// An attribute that represents a string.
/// </summary>
[PublicAPI]
public sealed class StringAttribute(string ns, string name) : ScalarAttribute<string, string, Utf8Serializer>(ns, name)
{
    /// <inheritdoc />
    public override string ToLowLevel(string value) => value;

    /// <inheritdoc />
    public override string FromLowLevel(string value, AttributeResolver resolver) => value;
}
