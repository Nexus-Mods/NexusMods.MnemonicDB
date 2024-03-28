using NexusMods.EventSourcing.Abstractions;

namespace NexusMods.EventSourcing.TestModel.Model.Attributes;

public class LoadoutAttributes
{
    /// <summary>
    ///     Name of the loadout
    /// </summary>
    public class Name : ScalarAttribute<Name, string>;

    /// <summary>
    ///     The last transaction that updated the loadout
    /// </summary>
    public class UpdatedTx : ScalarAttribute<UpdatedTx, TxId>;
}
