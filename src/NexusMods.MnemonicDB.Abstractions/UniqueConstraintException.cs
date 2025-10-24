using System;

namespace NexusMods.MnemonicDB.Abstractions;

/// <summary>
/// Thrown when a unique constraint on a attribute is
/// </summary>
public class UniqueConstraintException(Datom datom, EntityId id) 
    : Exception($"Unique constraint violation on datom: {datom} value is currently owned by {id}");
