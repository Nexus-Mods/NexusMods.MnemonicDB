using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Storage.Serializers;

namespace NexusMods.MnemonicDB.Storage;

/// <summary>
///    Built-in attributes that are always present in the database.
/// </summary>
public class BuiltInAttributes
{

    /// <summary>
    ///     The unique identifier of the entity, used to link attributes across application restarts and model changes.
    /// </summary>
    public static Attribute<Symbol> UniqueId = new ("NexusMods.MnemonicDB.DatomStore/UniqueId");

    /// <summary>
    ///      T
    /// </summary>
    public static Attribute<Symbol> ValueSerializerId = new ("NexusMods.MnemonicDB.DatomStore/ValueSerializerId");


    /// <summary>
    ///     The database entity id of the UniqueId attribute
    /// </summary>
    public static readonly AttributeId UniqueIdEntityId = AttributeId.From(1);

    /// <summary>
    ///     The database entity id of the UniqueId attribute
    /// </summary>
    private static readonly AttributeId ValueSerializerIdEntityId = AttributeId.From(2);


    /// <summary>
    ///     The initial set of built-in attributes that always exist in the database.
    /// </summary>
    public static readonly DbAttribute[] Initial =
    [
        new DbAttribute(UniqueId.Id, UniqueIdEntityId, SymbolSerializer.Id),
        new DbAttribute(ValueSerializerId.Id, ValueSerializerIdEntityId, SymbolSerializer.Id)
    ];

    public static readonly IWriteDatom[] InitialDatoms =
    [
        UniqueId.Assert(UniqueIdEntityId.ToEntityId(), UniqueId.Id),
        ValueSerializerId.Assert(UniqueIdEntityId.ToEntityId(), SymbolSerializer.Id),

        UniqueId.Assert(ValueSerializerIdEntityId.ToEntityId(), ValueSerializerId.Id),
        ValueSerializerId.Assert(ValueSerializerIdEntityId.ToEntityId(), SymbolSerializer.Id)
    ];


}
