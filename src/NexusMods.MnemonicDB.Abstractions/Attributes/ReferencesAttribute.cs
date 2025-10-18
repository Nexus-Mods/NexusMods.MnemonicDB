using System.Collections.Generic;
using JetBrains.Annotations;
using NexusMods.MnemonicDB.Abstractions.Models;
using NexusMods.MnemonicDB.Abstractions.ValueSerializers;

namespace NexusMods.MnemonicDB.Abstractions.Attributes;

/// <summary>
/// Represents a collection of references to other entities.
/// </summary>
[PublicAPI]
public class ReferencesAttribute(string ns, string name) : CollectionAttribute<EntityId, EntityId, EntityIdSerializer>(ns, name)
{
    /// <inheritdoc />
    public override EntityId ToLowLevel(EntityId value) => value;

    /// <inheritdoc />
    public override EntityId FromLowLevel(EntityId value, AttributeResolver resolver) => value;
}

/// <summary>
/// A typesafe reference attribute, that references entities of type T.
/// </summary>
[PublicAPI]
public sealed class ReferencesAttribute<T>(string ns, string name) : ReferencesAttribute(ns, name)
    where T : IModelDefinition
{
    public IEnumerable<TModel> GetAllModels<TModel>(IDb db, Datoms datoms) 
        where TModel : IReadOnlyModel<TModel>
    {
        var ids = datoms.GetAllResolved(this, db.Connection.AttributeResolver);
        foreach (var id in ids)
            yield return TModel.Create(db, id);
    }
}
