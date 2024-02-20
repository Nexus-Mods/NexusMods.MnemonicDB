using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Abstractions.Models;
using NexusMods.EventSourcing.TestModel.Model.Attributes;

namespace NexusMods.EventSourcing.TestModel.Model;

public class Loadout(ITransaction? tx) : AReadModel<Loadout>(tx)
{
    /// <summary>
    /// The name of the loadout.
    /// </summary>
    [From<LoadoutAttributes.Name>]
    public required string Name { get; init; }

    /// <summary>
    /// The last tx that updated the loadout.
    /// </summary>
    [From<LoadoutAttributes.UpdatedTx>]
    public required TxId Invalidator { get; init; }

    /// <summary>
    /// The mods in the loadout.
    /// </summary>
    public IEnumerable<Mod> Mods => GetReverse<ModAttributes.LoadoutId, Mod>();


    /// <summary>
    /// Create a new loadout with the given name.
    /// </summary>
    /// <param name="tx"></param>
    /// <param name="name"></param>
    /// <returns></returns>
    public static Loadout Create(ITransaction tx, string name)
    {
        return new Loadout(tx)
        {
            Name = name,
            Invalidator = tx.ThisTxId
        };
    }

    /// <summary>
    /// Updates this loadout marking it as touched by the given transaction.
    /// </summary>
    /// <param name="tx"></param>
    public void Touch(ITransaction tx)
    {
        LoadoutAttributes.UpdatedTx.Add(tx, Id, tx.ThisTxId);
    }
}
