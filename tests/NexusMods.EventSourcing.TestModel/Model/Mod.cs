using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Abstractions.Models;
using NexusMods.EventSourcing.TestModel.Model.Attributes;

namespace NexusMods.EventSourcing.TestModel.Model;

public class Mod(ITransaction? tx) : AReadModel<Mod>(tx)
{

    [From<ModAttributes.Name>]
    public required string Name { get; init; }


    [From<ModAttributes.LoadoutId>]
    public required EntityId LoadoutId { get; init; }

    /// <summary>
    /// The loadout for this mod.
    /// </summary>
    public Loadout Loadout => Get<Loadout>(LoadoutId);


    public static Mod Create(ITransaction tx, string name, EntityId loadoutId)
    {
        var mod = new Mod(tx)
        {
            Name = name,
            LoadoutId = loadoutId
        };
        return mod;
    }

    public void Touch(ITransaction tx)
    {
        Loadout.Touch(tx);
    }
}
