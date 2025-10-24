using System;
using JetBrains.Annotations;
using NexusMods.MnemonicDB.Abstractions.ValueSerializers;

namespace NexusMods.MnemonicDB.Abstractions.Attributes;

/// <summary>
/// An attribute that holds an int128 value.
/// </summary>
[PublicAPI]
public sealed class Int128Attribute(string ns, string name) : ScalarAttribute<Int128, Int128, Int128Serializer>(ns, name)
{
    /// <inheritdoc />
    public override Int128 ToLowLevel(Int128 value) => value;

    /// <inheritdoc />
    public override Int128 FromLowLevel(Int128 value, AttributeResolver resolver) => value;
}
