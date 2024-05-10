using NexusMods.MnemonicDB.Abstractions.TxFunctions;
using NexusMods.MnemonicDB.ModelReflection;
using NexusMods.MnemonicDB.TestModel;

namespace NexusMods.MnemonicDB.Tests;

public class ModelReflectionTests(IServiceProvider provider) : AMnemonicDBTest(provider)
{
    [Fact]
    public async Task CanGetAttributesFromModel()
    {

        var type = typeof(ILoadout);
        var reflector = new Reflector([
            typeof(ILoadout), typeof(ICollection), typeof(IFile), typeof(IMod), typeof(IArchiveFile)
        ]);

        reflector.Process();
        reflector.BuildAssembly();

        using var tx = Connection.BeginTransaction();
        var ent = tx.TempId();
        tx.Add(ent, ILoadout.Attributes.Name, "Test");
        var result = await tx.Commit();

        ent = result[ent];

        var loadout = reflector.MakeReadonly<ILoadout>(Connection.Db, ent);

        loadout.Name.Should().Be("Test");
        await Verify(reflector.Definitions);
    }
}
