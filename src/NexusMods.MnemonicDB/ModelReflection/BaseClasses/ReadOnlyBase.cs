using System;
using System.Collections.Generic;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.IndexSegments;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.MnemonicDB.ModelReflection.BaseClasses;

/// <summary>
/// Base class for read-only models.
/// </summary>
public class ReadOnlyBase(IDb db, EntityId id) : IModel
{
    private readonly IndexSegment _segment = db.Get(id);
    private readonly RegistryId _registryId = db.Registry.Id;
    public EntityId Id => id;

    public IDb Db => db;


    /// <summary>
    /// Reader for scalar attributes.
    /// </summary>
    protected TOuter Get<TOuter, TInner>(ScalarAttribute<TOuter, TInner> attr)
    {
        var dbId = attr.GetDbId(_registryId);

        for (var i = 0; i < _segment.Count; i++)
        {
            var datom = _segment[i];
            if (datom.A != dbId) continue;
            return attr.ReadValue(datom.ValueSpan);
        }

        ThrowKeyNotFoundException(attr);
        return default!;
    }

    private void ThrowKeyNotFoundException(IAttribute attr)
    {
        throw new KeyNotFoundException($"Attribute {attr} not found on entity {Id}");
    }
}
