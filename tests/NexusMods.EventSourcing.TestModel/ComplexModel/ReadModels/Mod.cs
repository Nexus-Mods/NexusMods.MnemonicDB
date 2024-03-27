using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Abstractions.Models;
using NexusMods.EventSourcing.TestModel.ComplexModel.Attributes;
using FileAttributes = NexusMods.EventSourcing.TestModel.ComplexModel.Attributes.FileAttributes;

namespace NexusMods.EventSourcing.TestModel.ComplexModel.ReadModels;

public class Mod(ITransaction? tx) : AReadModel<Mod>(tx)
{
    [From<ModAttributes.Name>] public required string Name { get; set; }

    [From<ModAttributes.Source>] public required Uri Source { get; set; }

    [From<ModAttributes.LoadoutId>] public required EntityId LoadoutId { get; init; }


    public IEnumerable<File> Files => GetReverse<FileAttributes.ModId, File>();

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
