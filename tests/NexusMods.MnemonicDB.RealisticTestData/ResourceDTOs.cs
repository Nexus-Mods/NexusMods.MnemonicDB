using System.Text.Json;
using ICSharpCode.SharpZipLib.BZip2;
using NexusMods.Hashing.xxHash64;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.RealisticTestData.Models;
using NexusMods.Paths;

namespace NexusMods.MnemonicDB.RealisticTestData;

public class ArchiveDTO
{
    public string Name { get; set; } = "";
    public ulong Hash { get; set; }
    public ulong Size { get; set; }
}

public class Directive
{
    public ulong ArchiveHash { get; set; }
    public ulong Size { get; set; }
    public ulong Hash { get; set; }
    public string To { get; set; } = "";
    public string ModName { get; set; } = "";
    public string ArchivePath { get; set; } = "";
}

public class ModState
{
    public string Name { get; set; } = "";
    public ulong Priority { get; set; }
    public bool Enabled { get; set; }
}

public class Root
{
    public ArchiveDTO[] Archives { get; set; } = [];
    public Directive[] Directives { get; set; } = [];
    public ModState[] ModStates { get; set; } = [];
    

    public static async Task<Root> FromJson()
    {
        var path = FileSystem.Shared.GetKnownPath(KnownPath.EntryDirectory)
            .Combine("Resources/modlist_small.bz2");

        await using var stream = new BZip2InputStream(path.Read());
        return (await JsonSerializer.DeserializeAsync<Root>(stream))!;
    }

    public static async Task<Loadout.ReadOnly> Import(IConnection conn)
    {
        var dataset = await FromJson();
        
        Dictionary<ArchiveId, Hash> archives = new();
        LoadoutId loadoutId = default;

        {
            
            
            using var tx = conn.BeginTransaction();

            var newLoadout = new Loadout.New(tx)
            {
                Name = Guid.NewGuid().ToString()
            };
            
            foreach (var archive in dataset.Archives)
            {
                var entity = new Archive.New(tx)
                {
                    Name = archive.Name,
                    Hash = Hash.From(archive.Hash),
                    Size = Size.From(archive.Size),
                    LoadoutId = newLoadout
                };
            }
            var result = await tx.Commit();

            loadoutId = result.Remap(newLoadout);
        }

        Dictionary<string, ModId> modMappings = new();
        {
            using var tx = conn.BeginTransaction();
            
            Dictionary<string, EntityId> modEntities = new();
            
            var baseMod = new Mod.New(tx)
            {
                Name = "__BASE__",
                Priority = 0,
                LoadoutId = loadoutId
            };
            
            modEntities.Add("__BASE__", baseMod);
            
            foreach (var mod in dataset.ModStates)
            {
                var entity = new Mod.New(tx)
                {
                    Name = mod.Name,
                    Priority = (uint)mod.Priority + 1,
                    LoadoutId = loadoutId
                };
                
                modEntities.Add(mod.Name, entity);
                
                if (mod.Enabled)
                {
                    tx.Add(entity.Id, Mod.Enabled, Null.Instance);
                }
            }
            
            var result = await tx.Commit();
            
            foreach (var (modName, modId) in modEntities)
            {
                modMappings.Add(modName, result[modId]);
            }
        }

        {
            using var tx = conn.BeginTransaction();
            foreach (var file in dataset.Directives)
            {

                
                var modId = modMappings[file.ModName];
                
                var entity = new ExtractedFile.New(tx)
                {
                    ArchiveHash = Hash.From(file.ArchiveHash),
                    ArchivePath = new RelativePath(file.ArchivePath),
                    Size = Size.From(file.Size),
                    Hash = Hash.From(file.Hash),
                    To = new RelativePath(file.To),
                    ModId = modId,
                    LoadoutId = loadoutId
                };

            }
                            
            await tx.Commit();
        }
        
        return Loadout.Load(conn.Db, loadoutId);
    }
}
