using System;
using System.Collections.Generic;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Abstractions.BuiltinEntities;

namespace NexusMods.EventSourcing.Sinks;

/// <summary>
/// Loads all the attributes from the result set
/// </summary>
public struct AttributeLoader : IResultSetSink
{
    /// <summary>
    /// Empty constructor
    /// </summary>
    public AttributeLoader()
    {
        Attributes = new List<DbRegisteredAttribute>();
        _currentName = "";
        _currentValueType = ValueTypes.String;
        _currentTypeId = UInt128.Zero;
    }

    public readonly List<DbRegisteredAttribute> Attributes;
    private string _currentName;
    private ValueTypes _currentValueType;
    private UInt128 _currentTypeId;

    public void Process<T>(ref T resultSet) where T : IResultSet
    {
        var currentAttrId = resultSet.EntityId;
        do
        {
            if (resultSet.EntityId != currentAttrId)
            {
                Attributes.Add(new DbRegisteredAttribute
                {
                    Name = _currentName,
                    ValueType = _currentValueType,
                    EntityTypeId = _currentTypeId,
                    Id = currentAttrId
                });
                currentAttrId = resultSet.EntityId;
            }

            switch (resultSet.Attribute)
            {
                case (ulong)AttributeIds.EntityTypeId:
                    if (resultSet.ValueUInt128 != DbRegisteredAttribute.StaticUniqueId)
                    {
                        throw new Exception("Bad attribute definition");
                    }
                    break;
                case (ulong)AttributeIds.Name:
                    _currentName = resultSet.ValueString;
                    break;
                case (ulong)AttributeIds.ValueType:
                    _currentValueType = (ValueTypes)resultSet.ValueUInt64;
                    break;
                case (ulong)AttributeIds.TypeId:
                    _currentTypeId = resultSet.ValueUInt128;
                    break;
                default:
                    break;
            }
        } while (resultSet.Next());
    }
}
