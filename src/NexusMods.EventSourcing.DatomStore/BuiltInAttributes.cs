using System;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.DatomStore.BuiltInSerializers;

namespace NexusMods.EventSourcing.DatomStore;

public static class BuiltInAttributes
{
    /// <summary>
    /// The unique identifier of the entity, used to link attributes across application restarts and model changes.
    /// </summary>
    public class UniqueId() : ScalarAttribute<UniqueId, Symbol>(UniqueIdStaticId);

    /// <summary>
    /// Static unique id of the UniqueId attribute
    /// </summary>
    private static readonly Symbol UniqueIdStaticId = Symbol.Intern("NexusMods.EventSourcing.DatomStore/UniqueId");

    /// <summary>
    /// The database entity id of the UniqueId attribute
    /// </summary>
    private static readonly ulong UniqueIdEntityId = 1;

    /// <summary>
    /// The unique id if the IValueSerializer used to serialize the value of the attribute.
    /// </summary>
    public class ValueSerializerId() : ScalarAttribute<ValueSerializerId, UInt128>("NexusMods.EventSourcing.DatomStore/ValueSerializerId");

    /// <summary>
    /// Static unique id of the UniqueId attribute
    /// </summary>
    private static readonly Symbol ValueSerializerIdStaticId = Symbol.Intern("NexusMods.EventSourcing.DatomStore/ValueSerializerId");

    /// <summary>
    /// The database entity id of the UniqueId attribute
    /// </summary>
    private static readonly ulong ValueSerializerIdEntityId = 2;


    /// <summary>
    /// The initial set of built-in attributes that always exist in the database.
    /// </summary>
    public static readonly DbAttribute[] Initial = [
        new DbAttribute(UniqueIdStaticId, UniqueIdEntityId, UInt128Serializer.Id),
        new DbAttribute(ValueSerializerIdStaticId, ValueSerializerIdEntityId, UInt128Serializer.Id),
    ];

    public static readonly IDatom[] InitialDatoms = [
        UniqueId.Assert(UniqueIdEntityId, UniqueIdStaticId),
        ValueSerializerId.Assert(UniqueIdEntityId, UInt128Serializer.Id),

        UniqueId.Assert(ValueSerializerIdEntityId, ValueSerializerIdStaticId),
        ValueSerializerId.Assert(ValueSerializerIdEntityId, UInt128Serializer.Id),
    ];

}
