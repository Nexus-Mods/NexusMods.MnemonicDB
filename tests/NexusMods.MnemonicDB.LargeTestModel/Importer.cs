using System.Text.Json;
using ICSharpCode.SharpZipLib.BZip2;
using NexusMods.Hashing.xxHash3;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.LargeTestModel.Models;
using NexusMods.Paths;

namespace NexusMods.MnemonicDB.LargeTestModel;

public static class Importer
{
    public static async Task<LargeLoadout.ReadOnly> Import(IConnection connection, AbsolutePath path)
    {
        await using var baseStream = path.Read();
        await using var bz2Stream = new BZip2InputStream(baseStream);
        var root = JsonSerializer.Deserialize<Root>(bz2Stream)!;

        var tx = connection.BeginTransaction();

        var loadout = new LargeLoadout.New(tx)
        {
            Name = "Large Modlist " + Guid.NewGuid(),
        };
        
        Dictionary<string, GroupId> groups = new();
        
        foreach (var mod in root.Mods)
        {
            var group = new Group.New(tx)
            {
                Name = mod.Key,
                LoadoutId = loadout,
            };
            
            if (!mod.Value) 
                group.IsDisabled = true;
            
            groups.Add(mod.Key, group);
        }
        
        var overridesGroup = new Group.New(tx)
        {
            Name = "Overrides",
            LoadoutId = loadout,
        };

        foreach (var directive in root.Directives)
        {
            if (!groups.TryGetValue(directive.Mod ?? "", out var group))
                group = overridesGroup;
            
            var file = new LoadoutItem.New(tx)
            {
                To = RelativePath.FromUnsanitizedInput(directive.To),
                LoadoutId = loadout,
                Size = directive.Size,
                Hash = Hash.FromULong(directive.Hash),
                GroupId = group
            };
        }

        foreach (var directive in root.Archives)
        {
            var archive = new Archive.New(tx)
            {
                Name = directive.Name,
                Size = directive.Size,
                Hash = Hash.FromULong(directive.Hash),
            };
        }

        var results = await tx.Commit();
        return results.Remap(loadout);
    }
}
