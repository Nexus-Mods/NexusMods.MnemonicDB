using NexusMods.MnemonicDB.ModelReflection;
using NexusMods.MnemonicDB.TestModel;

namespace NexusMods.MnemonicDB.Tests;

public class ModelReflectionTests
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


        await Verify(reflector.Definitions);
    }
}
