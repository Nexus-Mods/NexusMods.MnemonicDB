using System;

namespace NexusMods.EventSourcing.Abstractions.AttributeTypes;

/// <summary>
/// A string attribute type
/// </summary>
public class StringAttributeType : IAttributeType<string>
{
    /// <summary>
    /// Static unique identifier for the attribute type
    /// </summary>
    public static readonly UInt128 StaticUniqueId = "36DFF860-22BC-4C01-B962-1CF8EF576E90".GuidStringToUInt128();

    /// <inheritdoc />
    public string GetValue<TResultSet>(TResultSet resultSet, IDb db) where TResultSet : IResultSet
    {
        return resultSet.ValueString;
    }

    /// <inheritdoc />
    public void Emit<TSink>(ulong e, ulong a, string val, ulong t, IDatomSink sink) where TSink : IDatomSink
    {
        sink.Emit(e, a, val, t);
    }

    /// <inheritdoc />
    public ValueTypes ValueType => ValueTypes.String;

    /// <inheritdoc />
    public UInt128 UniqueId => StaticUniqueId;

    /// <inheritdoc />
    public Type DomainType => typeof(string);
}
