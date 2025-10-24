using JetBrains.Annotations;
using NexusMods.MnemonicDB.Abstractions.ValueSerializers;

namespace NexusMods.MnemonicDB.Abstractions.Attributes;

/// <summary>
/// An attribute that holds an int16 value.
/// </summary>
[PublicAPI]
public sealed class Int16Attribute(string ns, string name) : ScalarAttribute<short, short, Int16Serializer>(ns, name)
{
    /// <inheritdoc />
    public override short ToLowLevel(short value) => value;

    /// <inheritdoc />
    public override short FromLowLevel(short value, AttributeResolver resolver) => value;
}
