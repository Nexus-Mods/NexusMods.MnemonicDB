using System;

namespace NexusMods.EventSourcing.Abstractions;

/// <summary>
/// A sink for datoms, this is used to avoid overhead of using IEnumerable, casting and boxing
/// </summary>
public interface IDatomSink
{
    /// <summary>
    /// Emit a datom with a value.
    /// </summary>
    /// <param name="entityId"></param>
    /// <param name="attribute"></param>
    /// <param name="value"></param>
    public void Emit(EntityId entityId, ulong attribute, ulong value);

    public void Emit(EntityId entityId, ulong attribute, string value);

    /// <summary>
    /// Emit a datom with a reference.
    /// </summary>
    /// <param name="entityId"></param>
    /// <param name="attribute"></param>
    /// <param name="reference"></param>
    public void EmitReference(EntityId entityId, ulong attribute, EntityId reference);
}
