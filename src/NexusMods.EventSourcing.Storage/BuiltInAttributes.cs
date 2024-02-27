using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Storage.Serializers;

namespace NexusMods.EventSourcing.Storage;

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
    private static readonly AttributeId UniqueIdEntityId = AttributeId.From(1);

    /// <summary>
    /// The unique id if the IValueSerializer used to serialize the value of the attribute.
    /// </summary>
    public class ValueSerializerId() : ScalarAttribute<ValueSerializerId, Symbol>("NexusMods.EventSourcing.DatomStore/ValueSerializerId");

    /// <summary>
    /// Static unique id of the UniqueId attribute
    /// </summary>
    private static readonly Symbol ValueSerializerIdStaticId = Symbol.Intern("NexusMods.EventSourcing.DatomStore/ValueSerializerId");

    /// <summary>
    /// The database entity id of the UniqueId attribute
    /// </summary>
    private static readonly AttributeId ValueSerializerIdEntityId = AttributeId.From(2);


    /// <summary>
    /// The initial set of built-in attributes that always exist in the database.
    /// </summary>
    public static readonly DbAttribute[] Initial = [
        new DbAttribute(UniqueIdStaticId, UniqueIdEntityId, SymbolSerializer.Id),
        new DbAttribute(ValueSerializerIdStaticId, ValueSerializerIdEntityId, SymbolSerializer.Id),
    ];

    public static readonly ITypedDatom[] InitialDatoms = [
        UniqueId.Assert(UniqueIdEntityId.ToEntityId(), UniqueIdStaticId),
        ValueSerializerId.Assert(UniqueIdEntityId.ToEntityId(), SymbolSerializer.Id),

        UniqueId.Assert(ValueSerializerIdEntityId.ToEntityId(), ValueSerializerIdStaticId),
        ValueSerializerId.Assert(ValueSerializerIdEntityId.ToEntityId(), SymbolSerializer.Id),
    ];

}
