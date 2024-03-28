﻿using NexusMods.MneumonicDB.Abstractions;
using NexusMods.MneumonicDB.Abstractions.Models;
using NexusMods.MneumonicDB.TestModel.ComplexModel.Attributes;

namespace NexusMods.MneumonicDB.TestModel.ComplexModel.ReadModels;

public class Loadout(ITransaction? tx) : AReadModel<Loadout>(tx)
{
    [From<LoadoutAttributes.Name>] public string Name { get; set; } = string.Empty;

    [From<LoadoutAttributes.UpdatedAt>] public TxId UpdatedAt { get; set; } = TxId.Tmp;


    public IEnumerable<Mod> Mods => GetReverse<ModAttributes.LoadoutId, Mod>();

    /// <summary>
    ///     Creates a new loadout with the given name
    /// </summary>
    public static Loadout Create(ITransaction tx, string name)
    {
        var loadout = new Loadout(tx)
        {
            Name = name,
            UpdatedAt = TxId.Tmp
        };
        return loadout;
    }
}
