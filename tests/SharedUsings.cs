// ReSharper disable RedundantUsingDirective.Global

global using Xunit;
global using FluentAssertions;
using System.Runtime.CompilerServices;
using NexusMods.MnemonicDB.TestModel.Helpers;

public static class Initializer
{
    [ModuleInitializer]
    public static void Init()
    {
        VerifierSettings
            .AddExtraSettings(s =>
                s.Converters.Add(new ObjectTupleWriter()));
    }
}
