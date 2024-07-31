using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.MnemonicDB.Abstractions.Attributes;

/// <summary>
/// An attribute that references another entity.
/// </summary>
public class ReferenceAttribute(string ns, string name) : ScalarAttribute<EntityId, ulong>(ValueTags.Reference, ns, name)
{
    /// <inheritdoc />
    protected override ulong ToLowLevel(EntityId value) => value.Value;

    /// <inheritdoc />
    protected override EntityId FromLowLevel(ulong lowLevelType, ValueTags tags, RegistryId registryId) 
        => EntityId.From(lowLevelType);
}

/// <summary>
/// A typesafe reference attribute, that references entities of type T.
/// </summary>
public class ReferenceAttribute<T>(string ns, string name) : ReferenceAttribute(ns, name)
where T : IModelDefinition
{
}
