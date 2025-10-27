using JetBrains.Annotations;
using NexusMods.MnemonicDB.Abstractions.ValueSerializers;

namespace NexusMods.MnemonicDB.Abstractions.Attributes;

/// <summary>
/// An attribute that holds an uint64 value.
/// </summary>
[PublicAPI]
public sealed class Int64Attribute(string ns, string name) : ScalarAttribute<long, long, Int64Serializer>(ns, name)
{
    /// <inheritdoc />
    public override long ToLowLevel(long value) => value;

    /// <inheritdoc />
    public override long FromLowLevel(long value, AttributeResolver resolver) => value;
}
