using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.Models;
using NexusMods.MnemonicDB.Abstractions.TxFunctions;

namespace NexusMods.MnemonicDB.Abstractions.BuiltInEntities;

/// <summary>
/// Definition of an attribute, the data that is inserted into the
/// database.
/// </summary>
public partial class AttributeDefinition : IModelDefinition
{
    private const string Namespace = "NexusMods.MnemonicDB.DatomStore";

    /// <summary>
    /// The unique identifier of the entity, used to link attributes across application restarts and model changes.
    /// </summary>
    public static readonly SymbolAttribute UniqueId = new(Namespace, nameof(UniqueId)) { IsIndexed = true, IsUnique = true };

    /// <summary>
    /// The type of the value.
    /// </summary>
    public static readonly ValuesTagAttribute ValueType = new(Namespace, nameof(ValueType));

    /// <summary>
    /// True if the attribute is indexed.
    /// </summary>
    public static readonly MarkerAttribute Indexed = new(Namespace, nameof(Indexed));
    
    // Disabled until we figure out how to use TempIDs during attribute insertion
    /// <summary>
    /// True if the attribute is unique, that this attr/value pair can only exist on one entity at a time
    /// </summary>
    // public static readonly MarkerAttribute Unique = new(Namespace, nameof(Unique));

    /// <summary>
    /// This attribute is optional.
    /// </summary>
    public static readonly MarkerAttribute Optional = new(Namespace, nameof(Optional));

    /// <summary>
    /// Do not store history for this attribute.
    /// </summary>
    public static readonly MarkerAttribute NoHistory = new(Namespace, nameof(NoHistory));

    /// <summary>
    /// The cardinality of the attribute.
    /// </summary>
    public static readonly CardinalityAttribute Cardinality = new(Namespace, nameof(Cardinality));

    /// <summary>
    /// The doc string for the attribute
    /// </summary>
    public static readonly StringAttribute Documentation = new(Namespace, nameof(Documentation)) { IsOptional = true };

    /// <summary>
    /// Inserts an attribute into the transaction.
    /// </summary>
    public static void Insert(ITransaction tx, IAttribute attribute, ushort id = 0)
    {
        if (id == 0)
            id = HardcodedIds[attribute];
        var eid = EntityId.From(id);
        tx.Add(eid, UniqueId, attribute.Id);
        tx.Add(eid, ValueType, attribute.LowLevelType);
        tx.Add(eid, Cardinality, attribute.Cardinalty);
        if (attribute.IsIndexed)
            tx.Add(eid, Indexed, Null.Instance);
        //if (attribute.IsUnique)
        //    tx.Add(eid, Unique, Null.Instance);
        if (attribute.NoHistory)
            tx.Add(eid, NoHistory, Null.Instance);
        if (attribute.DeclaredOptional)
            tx.Add(eid, Optional, Null.Instance);
    }

    /// <summary>
    /// Hardcoded ids for the initial attributes
    /// </summary>
    public static readonly Dictionary<IAttribute, ushort> HardcodedIds = new()
    {
        { UniqueId, 1 },
        { ValueType, 2 },
        { Indexed, 3 },
        { Optional, 4 },
        { NoHistory, 5 },
        { Cardinality, 6 },
        { Documentation, 7 },
        { Transaction.Timestamp, 8},
        //{ Unique, 9 }
    };
    
    /// <summary>
    /// Adds the initial set of attributes to the transaction, these will be created when the transaction is committed.
    /// </summary>
    public static void AddInitial(ITransaction tx)
    {
        Insert(tx, UniqueId);
        Insert(tx, ValueType);
        Insert(tx, Documentation);
        Insert(tx, Indexed);
        //Insert(tx, Unique);
        Insert(tx, Optional);
        Insert(tx, NoHistory);
        Insert(tx, Cardinality);
        // Looks strange, but this is the best place to insert this so we don't duplicate all this insertion
        // logic in the transaction class as well.
        Insert(tx, Transaction.Timestamp);
    }
}
