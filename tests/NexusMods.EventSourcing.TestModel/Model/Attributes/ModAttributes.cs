using NexusMods.EventSourcing.Abstractions;

namespace NexusMods.EventSourcing.TestModel.Model.Attributes;

public class ModAttributes
{
    /// <summary>
    /// Name of the loadout
    /// </summary>
    public class Name : ScalarAttribute<Name, string>;

    /// <summary>
    /// The last transaction that updated the loadout
    /// </summary>
    public class UpdatedTx : ScalarAttribute<UpdatedTx, TxId>;

    /// <summary>
    /// The id of the loadout this mod belongs to
    /// </summary>
    public class LoadoutId : ScalarAttribute<LoadoutId, EntityId>;
}
