using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.Models;

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

/// <summary>
/// A typesafe reference attribute, that references entities of type T.
/// </summary>
public class ReferencesAttribute<T>(string ns, string name) : ReferencesAttribute(ns, name)
where T : IModelDefinition
{
}
