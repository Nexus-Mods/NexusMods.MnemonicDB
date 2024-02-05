using System.Collections.Generic;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Abstractions.BuiltinEntities;

namespace NexusMods.EventSourcing.Socket;

/// <summary>
/// A sink socket that emits new attribute definitions
/// </summary>
/// <param name="attributes"></param>
/// <param name="nextAttrId"></param>
/// <param name="tx"></param>
public class NewAttributeSinkSocket(IEnumerable<DbRegisteredAttribute> attributes, ulong tx) : IDatomSinkSocket
{
    public void Process<TSink>(ref TSink sink) where TSink : IDatomSink
    {
        foreach (var attribute in attributes)
        {
            sink.Emit(attribute.Id, (ulong)AttributeIds.EntityTypeId, DbRegisteredAttribute.StaticUniqueId, tx);
            sink.Emit(attribute.Id, (ulong)AttributeIds.Name, attribute.Name, tx);
            sink.Emit(attribute.Id, (ulong)AttributeIds.ValueType, (ulong)attribute.ValueType, tx);
            sink.Emit(attribute.Id, (ulong)AttributeIds.TypeId, attribute.EntityTypeId, tx);
        }
    }
}
