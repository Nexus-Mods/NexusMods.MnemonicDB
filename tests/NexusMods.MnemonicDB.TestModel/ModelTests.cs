using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;
using NexusMods.MnemonicDB.TestModel.Attributes;

namespace NexusMods.MnemonicDB.TestModel;

public class ModelTests
{
    public ModelTests()
    {
        ModelDefinition.New("TestModel")
        // The Name of the model
        .Attribute<StringAttribute>("Name", isIndexed: true)
        // The Source of the model
        .Attribute<UriAttribute>("Source")
        .Build();
    }

}
