using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.Models;
using NexusMods.MnemonicDB.Abstractions.ValueSerializers;

namespace NexusMods.MnemonicDB.Abstractions.Attributes;

/// <summary>
/// Represents a collection of references to other entities.
/// </summary>
public class ReferencesAttribute(string ns, string name) : CollectionAttribute<EntityId, EntityId, EntityIdSerializer>(ns, name)
{
    /// <inheritdoc />
    protected override EntityId ToLowLevel(EntityId value) => value;

    /// <inheritdoc />
    protected override EntityId FromLowLevel(EntityId value, AttributeResolver resolver) => value;
}

/// <summary>
/// A typesafe reference attribute, that references entities of type T.
/// </summary>
public sealed class ReferencesAttribute<T>(string ns, string name) : ReferencesAttribute(ns, name)
where T : IModelDefinition;
