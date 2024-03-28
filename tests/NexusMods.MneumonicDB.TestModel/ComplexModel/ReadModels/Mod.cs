using System.Text.Json.Serialization;
using NexusMods.MneumonicDB.Abstractions;
using NexusMods.MneumonicDB.Abstractions.Models;
using NexusMods.MneumonicDB.TestModel.ComplexModel.Attributes;
using FileAttributes = NexusMods.MneumonicDB.TestModel.ComplexModel.Attributes.FileAttributes;

namespace NexusMods.MneumonicDB.TestModel.ComplexModel.ReadModels;

public class Mod(ITransaction? tx) : AReadModel<Mod>(tx)
{
    [From<ModAttributes.Name>] public required string Name { get; set; }

    [From<ModAttributes.Source>] public required Uri Source { get; set; }

    [From<ModAttributes.LoadoutId>] public required EntityId LoadoutId { get; init; }


    public IEnumerable<File> Files => GetReverse<FileAttributes.ModId, File>();


    #region References

    public Loadout Loadout => Get<Loadout>(LoadoutId);

    public IEnumerable<Mod> Mods => GetReverse<ModAttributes.LoadoutId, Mod>();


    #endregion

    /// <summary>
    ///     Creates a new mod with the given name, source and loadout
    /// </summary>
    public static Mod Create(ITransaction tx, string name, Uri uri, Loadout loadout)
    {
        return new Mod(tx)
        {
            Name = name,
            Source = uri,
            LoadoutId = loadout.Id
        };
    }
}
