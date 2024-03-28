﻿using TransparentValueObjects;

namespace NexusMods.MneumonicDB.Abstractions;

/// <summary>
///     A unique identifier for an attribute, also an EntityId.
/// </summary>
[ValueObject<ulong>]
public readonly partial struct AttributeId
{
    /// <summary>
    ///     Converts the AttributeId to an EntityId.
    /// </summary>
    /// <returns></returns>
    public EntityId ToEntityId()
    {
        return EntityId.From(Value);
    }

    /// <summary>
    ///     Minimum value for an AttributeId.
    /// </summary>
    public static AttributeId Min => new(ulong.MinValue);
}
