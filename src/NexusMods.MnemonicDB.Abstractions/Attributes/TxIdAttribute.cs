using JetBrains.Annotations;
using NexusMods.MnemonicDB.Abstractions.ValueSerializers;

namespace NexusMods.MnemonicDB.Abstractions.Attributes;

/// <summary>
/// An attribute that holds a <see cref="TxId"/>.
/// </summary>
[PublicAPI]
public sealed class TxIdAttribute(string ns, string name) : ScalarAttribute<TxId, ulong, UInt64Serializer>(ns, name)
{
    /// <inheritdoc />
    protected override ulong ToLowLevel(TxId value) => value.Value;

    /// <inheritdoc />
    protected override TxId FromLowLevel(ulong value, AttributeResolver resolver) => TxId.From(value);
}
