using NexusMods.EventSourcing.Abstractions;

namespace NexusMods.EventSourcing.TestModel.ComplexModel.Attributes;

public class LoadoutAttributes
{
    /// <summary>
    ///     Name of the loadout
    /// </summary>
    public class Name : ScalarAttribute<Name, string>;

    /// <summary>
    ///     Incremented updated whenever any aspect of the loadout is changed
    /// </summary>
    public class UpdatedAt : ScalarAttribute<UpdatedAt, TxId>;
}
