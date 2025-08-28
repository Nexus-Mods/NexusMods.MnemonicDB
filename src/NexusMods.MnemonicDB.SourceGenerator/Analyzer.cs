using System.Collections.Generic;
using System.IO;
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
        if (context.SyntaxReceiver is not SyntaxReceiver syntaxReceiver) return;
        var modelAnalyzers = new Dictionary<INamedTypeSymbol, ModelAnalyzer>(capacity: syntaxReceiver.CandidateClasses.Count, comparer: SymbolEqualityComparer.Default);

        foreach (var candidate in syntaxReceiver.CandidateClasses)
        {
            var modelAnalyzer = new ModelAnalyzer(candidate, context);
            if (!modelAnalyzer.Analyze()) break;

            modelAnalyzers.Add(modelAnalyzer.ClassSymbol, modelAnalyzer);
        }

        var queue = new Queue<INamedTypeSymbol>();
        foreach (var current in modelAnalyzers.Values)
        {
            current.IncludedAttributes.AddRange(current.Attributes);

            queue.Clear();
            foreach (var includes in current.Includes)
            {
                queue.Enqueue(includes);
            }

            while (queue.Count > 0)
            {
                var namedTypeSymbol = queue.Dequeue();
                if (!modelAnalyzers.TryGetValue(namedTypeSymbol, out var modelAnalyzer)) continue;

                foreach (var includes in modelAnalyzer.Includes)
                {
                    queue.Enqueue(includes);
                }

                foreach (var attribute in modelAnalyzer.Attributes)
                {
                    current.IncludedAttributes.Add(attribute with
                    {
                        Name = $"{namedTypeSymbol.Name}_{attribute.Name}",
                    });
                }
            }
        }

        foreach (var modelAnalyzer in modelAnalyzers.Values)
        {
            var writer = new StringWriter();
            Templates.RenderModel(modelAnalyzer, writer);

            var full = modelAnalyzer.Namespace.ToDisplayString().Replace(".", "_") + "_" + modelAnalyzer.Name;
            context.AddSource($"{full}.Generated.cs", writer.ToString());
        }
    }
}
