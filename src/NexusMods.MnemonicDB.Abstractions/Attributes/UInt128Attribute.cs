using System;
using JetBrains.Annotations;
using NexusMods.MnemonicDB.Abstractions.ValueSerializers;

namespace NexusMods.MnemonicDB.Abstractions.Attributes;

/// <summary>
/// An attribute that holds an uint128 value.
/// </summary>
[PublicAPI]
public sealed class UInt128Attribute(string ns, string name) : ScalarAttribute<UInt128, UInt128, UInt128Serializer>(ns, name)
{
    /// <inheritdoc />
    protected override UInt128 ToLowLevel(UInt128 value) => value;

    /// <inheritdoc />
    protected override UInt128 FromLowLevel(UInt128 value, AttributeResolver resolver) => value;
}
