using NexusMods.MnemonicDB.Abstractions.ValueSerializers;

namespace NexusMods.MnemonicDB.Abstractions.Attributes;

/// <summary>
/// An attribute that represents a string.
/// </summary>
public sealed class StringAttribute(string ns, string name) : ScalarAttribute<string, string, Utf8Serializer>(ns, name)
{
    /// <inheritdoc />
    protected override string ToLowLevel(string value) => value;

    /// <inheritdoc />
    protected override string FromLowLevel(string value, AttributeResolver resolver) => value;
}
