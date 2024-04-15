using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.IndexSegments;
using NexusMods.MnemonicDB.Abstractions.Internals;
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
    public static Attribute<Symbol, string> UniqueId = new ("NexusMods.MnemonicDB.DatomStore/UniqueId");

    /// <summary>
    ///      T
    /// </summary>
    public static Attribute<ValueTags, byte> ValueType = new ("NexusMods.MnemonicDB.DatomStore/ValueType");


    /// <summary>
    ///     The database entity id of the UniqueId attribute
    /// </summary>
    public static readonly AttributeId UniqueIdEntityId = AttributeId.From(1);

    /// <summary>
    ///     The database entity id of the UniqueId attribute
    /// </summary>
    public static readonly AttributeId ValueTypeEntityId = AttributeId.From(2);


    /// <summary>
    ///     The initial set of built-in attributes that always exist in the database.
    /// </summary>
    public static readonly DbAttribute[] Initial =
    [
        new DbAttribute(UniqueId.Id, UniqueIdEntityId, ValueTags.Ascii),
        new DbAttribute(ValueType.Id, ValueTypeEntityId, ValueTags.UInt8)
    ];

    /// <summary>
    /// Gets the initial set of datoms for the built-in attributes.
    /// </summary>
    public static IndexSegment InitialDatoms(IAttributeRegistry registry)
    {
        var builder = new IndexSegmentBuilder(registry);

        builder.Add(UniqueIdEntityId.ToEntityId(), UniqueId, UniqueId.Id);
        builder.Add(ValueTypeEntityId.ToEntityId(), UniqueId, ValueSerializerId.Id);

        builder.Add(UniqueIdEntityId.ToEntityId(), ValueSerializerId, SymbolSerializer.Id);
        builder.Add(ValueTypeEntityId.ToEntityId(), ValueSerializerId, SymbolSerializer.Id);
        return builder.Build();
    }


}
