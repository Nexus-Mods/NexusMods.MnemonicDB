using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using NexusMods.MnemonicDB.Abstractions.Models;
using NexusMods.Paths;

namespace NexusMods.MnemonicDB.SourceGenerator.Tests;

public static class Helper
{
    public static async Task TestGenerator(
        [CallerFilePath] string sourceFile = "",
        params Type[] requiredTypes)
    {
        var generator = new ModelGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);

        var path = FileSystem.Shared.FromUnsanitizedFullPath(sourceFile);
        var inputPath = path.ReplaceExtension(new Extension(".input.cs"));
        await Assert.That(inputPath.FileExists).IsTrue();

        var input = await inputPath.ReadAllTextAsync();

        var compilation = CSharpCompilation.Create(
            typeof(Helper).Assembly.FullName,
            syntaxTrees: new[]
            {
                CSharpSyntaxTree.ParseText(input),
            },
            references: requiredTypes
                .Select(t => t.Assembly.Location)
                .Prepend(typeof(object).Assembly.Location) // .NET library
                .Prepend(typeof(IModelDefinition).Assembly.Location) // Abstraction library
                .Distinct()
                .Select(location => MetadataReference.CreateFromFile(location))
                .ToArray()
        );

        var result = driver.RunGenerators(compilation).GetRunResult();
        await Assert.That(result.GeneratedTrees.Length).IsGreaterThan(0);

        // ReSharper disable once ExplicitCallerInfoArgument
        await Verify(result, sourceFile: sourceFile).UseFileName(path.GetFileNameWithoutExtension());
    }
}
