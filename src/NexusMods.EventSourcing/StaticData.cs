using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Abstractions.AttributeTypes;
using NexusMods.EventSourcing.Abstractions.BuiltinEntities;

namespace NexusMods.EventSourcing;

enum AttributeIds : ulong
{
    EntityTypeId = 1,
    Name = 2,
    TypeId = 3,
    ValueType = 4,
    Owner = 5,
    TxTimestamp = 6
}

class StaticData
{
    public static (ulong E, ulong A, object V, ulong Tx)[] InitialState()
    {
        var attrEntityTypeId = (ulong)AttributeIds.EntityTypeId;
        var attrName = (ulong)AttributeIds.Name;
        var attrTypeId = (ulong)AttributeIds.TypeId;
        var attrValueType = (ulong)AttributeIds.ValueType;
        var attrOwner = (ulong)AttributeIds.Owner;
        var attrTxTimestamp = (ulong)AttributeIds.TxTimestamp;

        var txId = Ids.MinId(IdSpace.Tx);

        return
        [
            // Attribute.$type - aka DbRegisteredAttribute
            (attrEntityTypeId, attrEntityTypeId, DbRegisteredAttribute.StaticUniqueId, txId),
            (attrEntityTypeId, attrName, "$type", txId),
            (attrEntityTypeId, attrTypeId, UInt128AttributeType.StaticUniqueId, txId),
            (attrEntityTypeId, attrValueType, (ulong)ValueTypes.UHugeInt, txId),

            // Attribute.Name
            (attrName, attrEntityTypeId, DbRegisteredAttribute.StaticUniqueId, txId),
            (attrName, attrName, "Name", txId),
            (attrName, attrTypeId, StringAttributeType.StaticUniqueId, txId),
            (attrName, attrValueType, (ulong)ValueTypes.String, txId),

            // Attribute.TypeId
            (attrTypeId, attrEntityTypeId, DbRegisteredAttribute.StaticUniqueId, txId),
            (attrTypeId, attrName, "TypeId", txId),
            (attrTypeId, attrTypeId, StringAttributeType.StaticUniqueId, txId),
            (attrTypeId, attrValueType, (ulong)ValueTypes.UHugeInt, txId),

            // Attribute.ValueType
            (attrValueType, attrEntityTypeId, DbRegisteredAttribute.StaticUniqueId, txId),
            (attrValueType, attrName, "ValueType", txId),
            (attrValueType, attrTypeId, StringAttributeType.StaticUniqueId, txId),
            (attrValueType, attrValueType, (ulong)ValueTypes.UInt64, txId),
        ];



    }
}
