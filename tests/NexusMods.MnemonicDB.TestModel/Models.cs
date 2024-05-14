using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;
using NexusMods.MnemonicDB.TestModel.Attributes;

namespace NexusMods.MnemonicDB.TestModel;

public static class Models
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
            .Attribute<RelativePathAttribute>("Path")
            .Attribute<HashAttribute>("Hash")
            .Attribute<SizeAttribute>("Size")
            .Reference<Mod>("Mod")
            .Build();

        ModelDefinition.New("Loadout")
            .Attribute<StringAttribute>("Name")
            .Build();

        ModelDefinition.New("Collection")
            .Attribute<StringAttribute>("Name")
            .References<Mod>("Mod")
            .Reference<Loadout>("Loadout")
            .Build();
    }

}
