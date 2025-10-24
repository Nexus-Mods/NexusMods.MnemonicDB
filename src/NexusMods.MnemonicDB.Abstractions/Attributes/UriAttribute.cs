using System;
using JetBrains.Annotations;
using NexusMods.MnemonicDB.Abstractions.ValueSerializers;

namespace NexusMods.MnemonicDB.Abstractions.Attributes;

/// <summary>
/// An attribute that holds a <see cref="Uri"/>.
/// </summary>
[PublicAPI]
public sealed class UriAttribute(string ns, string name) : ScalarAttribute<Uri, string, Utf8Serializer>(ns, name)
{
    /// <inheritdoc />
    public override string ToLowLevel(Uri value) => value.ToString();

    /// <inheritdoc />
    public override Uri FromLowLevel(string value, AttributeResolver resolver) => new(value);
}
