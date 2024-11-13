using JetBrains.Annotations;
using NexusMods.MnemonicDB.Abstractions.ValueSerializers;

namespace NexusMods.MnemonicDB.Abstractions.Attributes;

/// <summary>
/// An attribute that holds an int32 value.
/// </summary>
[PublicAPI]
public sealed class Int32Attribute(string ns, string name) : ScalarAttribute<int, int, Int32Serializer>(ns, name)
{
    /// <inheritdoc />
    protected override int ToLowLevel(int value) => value;

    /// <inheritdoc />
    protected override int FromLowLevel(int value, AttributeResolver resolver) => value;
}
