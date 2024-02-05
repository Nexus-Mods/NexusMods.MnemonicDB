using System;

namespace NexusMods.EventSourcing.Abstractions.AttributeTypes;

/// <summary>
/// A UInt128 attribute type
/// </summary>
public class UInt128AttributeType : IAttributeType<UInt128>
{
    /// <inheritdoc />
    public UInt128 GetValue(IResultSet resultSet)
    {
        return resultSet.ValueUInt128;
    }

    /// <inheritdoc />
    public void Emit<TSink>(ulong e, ulong a, UInt128 val, ulong t, IDatomSink sink) where TSink : IDatomSink
    {
        sink.Emit(e, a, val, t);
    }

    /// <inheritdoc />
    public ValueTypes ValueType => ValueTypes.UHugeInt;

    /// <summary>
    /// Static unique identifier for the attribute type
    /// </summary>
    public static UInt128 StaticUniqueId = "F8D758A2-B076-4FBC-97BD-4C3F9DCD71A4".GuidStringToUInt128();

    /// <inheritdoc />
    public UInt128 UniqueId => StaticUniqueId;

    /// <inheritdoc />
    public Type DomainType => typeof(UInt128);
}
