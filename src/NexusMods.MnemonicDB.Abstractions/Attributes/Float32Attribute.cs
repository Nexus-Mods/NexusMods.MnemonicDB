using JetBrains.Annotations;
using NexusMods.MnemonicDB.Abstractions.ValueSerializers;

namespace NexusMods.MnemonicDB.Abstractions.Attributes;

/// <summary>
/// An attribute that holds a float32 value.
/// </summary>
[PublicAPI]
public sealed class Float32Attribute(string ns, string name) : ScalarAttribute<float, float, Float32Serializer>(ns, name)
{
    /// <inheritdoc />
    public override float ToLowLevel(float value) => value;

    /// <inheritdoc />
    public override float FromLowLevel(float value, AttributeResolver resolver) => value;
}
