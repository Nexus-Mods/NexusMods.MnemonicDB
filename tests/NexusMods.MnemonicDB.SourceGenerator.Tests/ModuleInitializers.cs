using System.Runtime.CompilerServices;

namespace NexusMods.MnemonicDB.SourceGenerator.Tests;

public static class ModuleInitializer
{
    [ModuleInitializer]
    public static void Init()
    {
        VerifySourceGenerators.Initialize();
    }
}
