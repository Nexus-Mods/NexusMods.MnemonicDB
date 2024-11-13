using JetBrains.Annotations;
using NexusMods.MnemonicDB.Abstractions.ValueSerializers;

namespace NexusMods.MnemonicDB.Abstractions.Attributes;

/// <summary>
/// An attribute that holds a float64 value.
/// </summary>
[PublicAPI]
public sealed class Float64Attribute(string ns, string name) : ScalarAttribute<double, double, Float64Serializer>(ns, name)
{
    /// <inheritdoc />
    protected override double ToLowLevel(double value) => value;

    /// <inheritdoc />
    protected override double FromLowLevel(double value, AttributeResolver resolver) => value;
}
