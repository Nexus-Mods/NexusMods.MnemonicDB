using System;

namespace NexusMods.EventSourcing.Abstractions.AttributeTypes;

public class EntityLinkAttributeType<T> : IAttributeType<EntityLink<T>> where T : AEntity
{
    private static readonly UInt128 _staticUniqueId = "5FC83238-EBC7-4561-9CC0-41065E5E973E".GuidStringToUInt128();

    public ValueTypes ValueType => ValueTypes.UInt64;
    public UInt128 UniqueId => _staticUniqueId;
    public Type DomainType => typeof(EntityLink<T>);

    public EntityLink<T> GetValue<TResultSet>(TResultSet resultSet, IDb db) where TResultSet : IResultSet
    {
        return new EntityLink<T>(EntityId<T>.From(resultSet.ValueUInt64), db);
    }

    public void Emit<TSink>(ulong e, ulong a, EntityLink<T> val, ulong t, IDatomSink sink) where TSink : IDatomSink
    {
        sink.Emit(e, a, val.Id, t);
    }
}
