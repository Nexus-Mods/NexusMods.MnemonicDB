using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.IndexSegments;
using NexusMods.MnemonicDB.Abstractions.Internals;

namespace NexusMods.MnemonicDB.Storage;

/// <summary>
///    Built-in attributes that are always present in the database.
/// </summary>
public class BuiltInAttributes
{

    private const string Namespace = "NexusMods.MnemonicDB.DatomStore";
    private const string TxNamespace = "NexusMods.MnemonicDB.Transaction";

    /// <summary>
    ///     The unique identifier of the entity, used to link attributes across application restarts and model changes.
    /// </summary>
    public static SymbolAttribute UniqueId = new (Namespace, "UniqueId");

    /// <summary>
    ///      T
    /// </summary>
    public static ValuesTagAttribute ValueType = new (Namespace, "ValueType");


    /// <summary>
    /// The timestamp of the transaction
    /// </summary>
    public static TimestampAttribute TxTimestanp = new(TxNamespace, "Timestamp");


    /// <summary>
    ///     The database entity id of the UniqueId attribute
    /// </summary>
    public static readonly AttributeId UniqueIdEntityId = AttributeId.From(1);

    /// <summary>
    ///     The database entity id of the UniqueId attribute
    /// </summary>
    public static readonly AttributeId ValueTypeEntityId = AttributeId.From(2);

    /// <summary>
    ///     The tx timestamp attribute
    /// </summary>
    public static readonly AttributeId TxTimestanpEntityId = AttributeId.From(3);

    /// <summary>
    ///     The initial set of built-in attributes that always exist in the database.
    /// </summary>
    public static readonly DbAttribute[] Initial =
    [
        new DbAttribute(UniqueId.Id, UniqueIdEntityId, ValueTags.Ascii),
        new DbAttribute(ValueType.Id, ValueTypeEntityId, ValueTags.UInt8),
        new DbAttribute(TxTimestanp.Id, TxTimestanpEntityId, ValueTags.Int64),
    ];

    /// <summary>
    /// Gets the initial set of datoms for the built-in attributes.
    /// </summary>
    public static IndexSegment InitialDatoms(IAttributeRegistry registry)
    {
        var builder = new IndexSegmentBuilder(registry);

        builder.Add(UniqueIdEntityId.ToEntityId(), UniqueId, UniqueId.Id);
        builder.Add(ValueTypeEntityId.ToEntityId(), UniqueId, ValueType.Id);
        builder.Add(TxTimestanpEntityId.ToEntityId(), UniqueId, TxTimestanp.Id);

        builder.Add(UniqueIdEntityId.ToEntityId(), ValueType, ValueTags.Ascii);
        builder.Add(ValueTypeEntityId.ToEntityId(), ValueType, ValueTags.UInt8);
        builder.Add(TxTimestanpEntityId.ToEntityId(), ValueType, ValueTags.Int64);

        return builder.Build();
    }


}
