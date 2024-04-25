using TransparentValueObjects;

namespace NexusMods.MnemonicDB.Abstractions;

/// <summary>
///     A unique identifier for an attribute, also an EntityId.
/// </summary>
[ValueObject<ushort>]
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
    public static AttributeId Min => new(ushort.MinValue);

    /// <summary>
    ///     Maximum value for an AttributeId.
    /// </summary>
    public static AttributeId Max => new(ushort.MaxValue);

    /// <inheritdoc />
    public override string ToString()
    {
        return "AId:" + Value.ToString("X");
    }
}
