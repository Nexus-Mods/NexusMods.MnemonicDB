using NexusMods.MnemonicDB.Abstractions.ElementComparers;

namespace NexusMods.MnemonicDB.Abstractions.Attributes;

/// <summary>
/// Represents a collection of references to other entities.
/// </summary>
public class ReferencesAttribute(string ns, string name) : CollectionAttribute<EntityId, ulong>(ValueTags.Reference, ns, name)
{
    /// <inheritdoc />
    protected override ulong ToLowLevel(EntityId value) => value.Value;

    /// <inheritdoc />
    protected override EntityId FromLowLevel(ulong value, ValueTags tags) => EntityId.From(value);
}
