using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.MnemonicDB.Abstractions.Attributes;

/// <summary>
/// An attribute that represents a collection of values
/// </summary>
[PublicAPI]
public abstract class CollectionAttribute<TValue, TLowLevel, TSerializer>(string ns, string name)
    : Attribute<TValue, TLowLevel, TSerializer>(ns, name, cardinality: Cardinality.Many) 
    where TSerializer : IValueSerializer<TLowLevel> 
    where TValue : notnull
    where TLowLevel : notnull;
