﻿using System;
using System.Buffers;
using System.Collections.Generic;

namespace NexusMods.MnemonicDB.Abstractions.Internals;

/// <summary>
///     A registry of attributes and serializers that supports operations that requires converting
///     between the database IDs, the code-level attributes and the native values
/// </summary>
public interface IAttributeRegistry
{
    /// <summary>
    ///     Resolve the given KeyPrefix + Value into a datom
    /// </summary>
    /// <param name="datom"></param>
    /// <returns></returns>
    public IReadDatom Resolve(in KeyPrefix prefix, ReadOnlySpan<byte> datom);
    
    
    /// <summary>
    /// Resolve the given attribute id into an attribute
    /// </summary>
    public IAttribute GetAttribute(AttributeId attributeId);

    /// <summary>
    /// Populates the registry with the given attributes, mostly used for
    /// internal registration of attributes
    /// </summary>
    /// <param name="attributes"></param>
    public void Populate(IEnumerable<DbAttribute> attributes);

    /// <summary>
    /// The registry id of the registry, this can be used to link attributes to attribute ids.
    /// A separate registry id is used for each registry instance and backing datom store.
    /// </summary>
    public RegistryId Id { get; }
}
