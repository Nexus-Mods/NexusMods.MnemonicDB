using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Microsoft.CodeAnalysis;

namespace NexusMods.MnemonicDB.SourceGenerator;

/// <summary>
/// Generates the model classes from the given contexts
/// </summary>
[Generator]
public class ModelGenerator : ISourceGenerator
{
    /// <inheritdoc />
    public void Initialize(GeneratorInitializationContext context)
    {
        context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
    }

    /// <inheritdoc />
    public void Execute(GeneratorExecutionContext context)
    {
        List<ModelAnalyzer> modelAnalyzers = new();
        foreach (var candidate in ((SyntaxReceiver)context.SyntaxReceiver!).CandidateClasses)
        {
            var modelAnalyzer = new ModelAnalyzer(candidate, context);
            if (!modelAnalyzer.Analyze())
                break;

            modelAnalyzers.Add(modelAnalyzer);
        }

        foreach (var modelAnalyzer in modelAnalyzers)
        {
            var writer = new StringWriter();
            Templates.RenderModel(modelAnalyzer, writer);

            var full = modelAnalyzer.Namespace.ToDisplayString().Replace(".", "_") + "_" + modelAnalyzer.Name;
            context.AddSource($"{full}.Generated.cs", writer.ToString());
        }
        return;

    }
}
