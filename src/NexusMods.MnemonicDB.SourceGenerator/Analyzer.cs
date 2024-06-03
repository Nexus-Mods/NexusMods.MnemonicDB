using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.CodeAnalysis;

namespace NexusMods.MnemonicDB.SourceGenerator;

[Generator]
public class ModelGenerator : ISourceGenerator
{
    public void Initialize(GeneratorInitializationContext context)
    {
        context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
    }

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
            context.AddSource($"{modelAnalyzer.Name}.Generated.cs", writer.ToString());
        }
        return;

    }
}
