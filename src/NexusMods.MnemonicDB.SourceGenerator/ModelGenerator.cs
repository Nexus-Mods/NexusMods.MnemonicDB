using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

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
    if (context.SyntaxReceiver is not SyntaxReceiver receiver)
        return;

    var compilation = context.Compilation;
    var modelDefinitionSymbol = compilation.GetTypeByMetadataName("NexusMods.MnemonicDB.Abstractions.Models.ModelDefinition");

    var chains = new Dictionary<string, MethodChain>();

    var buildInvocations = receiver.Invocations
        .Where(i => (compilation.GetSemanticModel(i.SyntaxTree).GetSymbolInfo(i).Symbol as IMethodSymbol)?.Name == "Build");

    foreach (var buildInvocation in buildInvocations)
    {
        var chain = new List<MethodCall>();
        var currentInvocation = buildInvocation;

        string? ns = null;
        while (currentInvocation != null)
        {
            var semanticModel = compilation.GetSemanticModel(currentInvocation.SyntaxTree);
            var symbol = semanticModel.GetSymbolInfo(currentInvocation).Symbol;
            ns ??= symbol?.ContainingNamespace.ToDisplayString();

            if (symbol?.ContainingType.Equals(modelDefinitionSymbol, SymbolEqualityComparer.Default) == true)
            {
                var info = new MethodCall
                {
                    MethodName = symbol.Name,
                    GenericTypes = (symbol as IMethodSymbol)?.TypeArguments
                        .Select(t => t.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)).ToList(),
                    Arguments = currentInvocation.ArgumentList.Arguments
                        .Select(a => semanticModel.GetConstantValue(a.Expression).Value!).ToList()
                };

                chain.Insert(0, info); // Insert at the beginning to reverse the order
            }

            // Use the Expression property to traverse up the chain
            currentInvocation = (currentInvocation.Expression as MemberAccessExpressionSyntax)?.Expression as InvocationExpressionSyntax;
        }

        if (chain.First().MethodName == "New")
        {
            var chainName = chain[0].MethodName;
            chains[chainName] = new MethodChain
            {
                Namespace = ns!,
                Methods = chain.ToArray()
            };
        }
    }
}

    private class SyntaxReceiver : ISyntaxReceiver
    {
        public List<InvocationExpressionSyntax> Invocations { get; } = new();

        public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
        {
            if (syntaxNode is InvocationExpressionSyntax invocation)
                Invocations.Add(invocation);
        }
    }
}
