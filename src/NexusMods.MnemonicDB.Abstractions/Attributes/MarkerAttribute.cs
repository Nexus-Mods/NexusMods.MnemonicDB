using JetBrains.Annotations;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.ValueSerializers;

namespace NexusMods.MnemonicDB.Abstractions.Attributes;

/// <summary>
/// An attribute that doesn't have a value, but is used to dispatch logic or to mark an entity
/// as being of a certain type.
/// </summary>
[PublicAPI]
public sealed class MarkerAttribute(string ns, string name) : ScalarAttribute<Null, Null, NullSerializer>(ns, name)
{
    /// <inheritdoc />
    public override Null ToLowLevel(Null value) => value;

    /// <inheritdoc />
    public override Null FromLowLevel(Null value, AttributeResolver resolver) => value;
}
