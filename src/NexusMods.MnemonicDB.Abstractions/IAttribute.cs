﻿using System;
using System.Buffers;
using NexusMods.MnemonicDB.Abstractions.BuiltInEntities;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.Internals;
using NexusMods.MnemonicDB.Abstractions.Models;
using NexusMods.MnemonicDB.Abstractions.Traits;

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
    ///    True if the attribute is unique, false if it is not. Unique here is global for a specific attribute, this can
    /// be thought of as making sure that .Datoms(Attribute, Value) for this attribute never returns more than one datom.
    /// </summary>
    bool IsUnique { get; }
    
    /// <summary>
    /// The indexed flags for the attribute
    /// </summary>
    IndexedFlags IndexedFlags { get; }

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
    /// The shorthand version of the id which is the last segment of the namespace and the name, e.g. `Mod/Name`
    /// </summary>
    string ShortName { get; }
    
    /// <summary>
    ///     Returns true if the attribute is in the given entity
    /// </summary>
    bool IsIn<T>(T entity) where T : IHasIdAndEntitySegment;

    /// <summary>
    /// Returns the high level value converted from the low level value
    /// </summary>
    object FromLowLevelObject(object o, AttributeResolver resolver);
}

/// <summary>
/// An interface for attributes that can write a given high level type to a buffer
/// </summary>
public interface IWritableAttribute<in THighLevelType> : IAttribute
{
    /// <summary>
    /// Write the given datom parts to the buffer
    /// </summary>
    public void Write<TWriter>(EntityId entityId, AttributeCache cache, THighLevelType value, TxId txId, bool isRetract, TWriter writer) 
        where TWriter : IBufferWriter<byte>;
}


/// <summary>
/// A readable attribute that has the given type as its value
/// </summary>
public interface IReadableAttribute<out T> : IAttribute
{
    /// <summary>
    /// Reads the high level value from the given span
    /// </summary>
    public T ReadValue(ReadOnlySpan<byte> span, ValueTag tag, AttributeResolver resolver);
}


/// <summary>
/// Combiner interface for both readable and writable attributes
/// </summary>
public interface IAttribute<T> : IReadableAttribute<T>, IWritableAttribute<T>
{
    
}
