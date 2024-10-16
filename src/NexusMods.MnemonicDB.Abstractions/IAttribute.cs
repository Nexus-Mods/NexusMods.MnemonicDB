﻿using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.Internals;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.MnemonicDB.Abstractions;

/// <summary>
///     Interface for a specific attribute
/// </summary>
public interface IAttribute
{
    /// <summary>
    ///     The native C# type of the value.
    /// </summary>
    public Type ValueType { get; }


    /// <summary>
    /// The low-level (MnemonicDB) type of the value.
    /// </summary>
    public ValueTag LowLevelType { get; }

    /// <summary>
    ///     True if the attribute's value is a reference to another entity, false if it is a value type.
    /// </summary>
    public bool IsReference { get; }

    /// <summary>
    ///     The Unique identifier of the attribute, this is used to match the attribute to a matching attribute
    ///     in the datastore
    /// </summary>
    public Symbol Id { get; }
    
    /// <summary>
    ///    True if the attribute is indexed, false if it is not.
    /// </summary>
    bool IsIndexed { get; }

    /// <summary>
    ///   True if the attribute has no history, false if it does.
    /// </summary>
    bool NoHistory { get; }

    /// <summary>
    ///   True if the attribute is optional, false if it is not.
    /// </summary>
    bool DeclaredOptional { get; }

    /// <summary>
    ///   The cardinality of the attribute
    /// </summary>
    Cardinality Cardinalty { get; }

    /// <summary>
    ///     Converts the given values into a typed datom
    /// </summary>
    IReadDatom Resolve(in KeyPrefix prefix, ReadOnlySpan<byte> valueSpan, AttributeResolver resolver);
    
    /// <summary>
    /// Adds the value to the transaction on the given entity/attribute, assumes the value is of the correct type
    /// </summary>
    void Add(ITransaction tx, EntityId entityId, object value, bool isRetract);

    /// <summary>
    ///     Returns true if the attribute is in the given entity
    /// </summary>
    bool IsIn<T>(T entity) where T : IHasIdAndIndexSegment;
}


/// <summary>
/// An attribute that has a specific value type
/// </summary>
/// <typeparam name="TValueType"></typeparam>
public interface IAttribute<TValueType> : IAttribute
{
    /// <summary>
    /// Adds the value to the transaction on the given entity/attribute
    /// </summary>
    public void Add(ITransaction tx, EntityId entityId, TValueType value, bool isRetract);
    
}
