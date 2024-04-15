using NexusMods.MnemonicDB.Abstractions.ElementComparers;

namespace NexusMods.MnemonicDB.Abstractions.Attributes;

/// <summary>
/// An attribute that references another entity.
/// </summary>
public class ReferenceAttribute(string ns, string name) : ScalarAttribute<EntityId, ulong>(ValueTags.Reference, ns, name)
{
    /// <inheritdoc />
    protected override ulong ToLowLevel(EntityId value) => value.Value;

    /// <inheritdoc />
    protected override EntityId FromLowLevel(ulong lowLevelType, ValueTags tags) => EntityId.From(lowLevelType);
}
