using NexusMods.MnemonicDB.Abstractions.Models;
using NexusMods.MnemonicDB.Abstractions.ValueSerializers;

namespace NexusMods.MnemonicDB.Abstractions.Attributes;

/// <summary>
/// An attribute that references another entity.
/// </summary>
public class ReferenceAttribute(string ns, string name) : ScalarAttribute<EntityId, EntityId, EntityIdSerializer>(ns, name)
{
    /// <inheritdoc />
    protected override EntityId ToLowLevel(EntityId value) => value;

    /// <inheritdoc />
    protected override EntityId FromLowLevel(EntityId value, AttributeResolver resolver) => value;
}

/// <summary>
/// A typesafe reference attribute, that references entities of type T.
/// </summary>
public sealed class ReferenceAttribute<T>(string ns, string name) : ReferenceAttribute(ns, name)
where T : IModelDefinition;
