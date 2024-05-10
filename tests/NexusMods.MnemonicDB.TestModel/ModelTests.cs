using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;
using NexusMods.MnemonicDB.TestModel.Attributes;

namespace NexusMods.MnemonicDB.TestModel;

public class ModelTests
{
    public ModelTests()
    {
        ModelDefinition.New("TestModel")
        .WithAttribute<StringAttribute>("Name", isIndexed: true)
        .WithAttribute<UriAttribute>("Source")
        .WithAttribute<ReferenceAttribute>("LoadoutId")
        .WithAttribute<MarkerAttribute>("IsMarked")
        .WithBackReference<Loadout.Model>("Loadout")
        .Build();
    }

}
