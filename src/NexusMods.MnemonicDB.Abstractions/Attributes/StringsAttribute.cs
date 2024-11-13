using JetBrains.Annotations;
using NexusMods.MnemonicDB.Abstractions.ValueSerializers;

namespace NexusMods.MnemonicDB.Abstractions.Attributes;

/// <summary>
/// An attribute that holds a collection of strings.
/// </summary>
[PublicAPI]
public sealed class StringsAttribute(string ns, string name) : CollectionAttribute<string, string, Utf8Serializer>(ns, name)
{
    /// <inheritdoc/>
    protected override string ToLowLevel(string value) => value;

    /// <inheritdoc/>
    protected override string FromLowLevel(string value, AttributeResolver resolver) => value;
}
