using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;
using NexusMods.MnemonicDB.TestModel.Attributes;

namespace NexusMods.MnemonicDB.TestModel;

public static partial class Models
{
    public static void RegisterModels()
    {
        ModelDefinition.New("Mod")
            // The Name of the mod
            .Attribute<StringAttribute>("Name", isIndexed: true)
            // The Source of the model
            .Attribute<UriAttribute>("Source")
            .Reference<Loadout>("Loadout")
            .Build();

        ModelDefinition.New("File")
            .Attribute<RelativePathAttribute>("Path", isIndexed: true)
            .Attribute<HashAttribute>("Hash", isIndexed: true)
            .Attribute<SizeAttribute>("Size")
            .Reference<Mod>("Mod")
            .Build();

        ModelDefinition.New("ArchiveFile")
            .Attribute<RelativePathAttribute>("Path")
            .Attribute<HashAttribute>("Hash")
            .Build();

        ModelDefinition.New("Loadout")
            .Attribute<StringAttribute>("Name")
            .BackRef<Mod>("Loadout", "Mods")
            .Build();

        ModelDefinition.New("Collection")
            .Attribute<StringAttribute>("Name")
            .References<Mod>("Mod")
            .Reference<Loadout>("Loadout")
            .Build();
    }

}
