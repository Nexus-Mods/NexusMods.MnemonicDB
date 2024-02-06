using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NexusMods.EventSourcing.EntityGenerator;

[Generator]
public class Generator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var provider = context.SyntaxProvider.CreateSyntaxProvider(
            predicate: static (p, _) => p is ClassDeclarationSyntax,
            transform: static (ctx, _) => (ClassDeclarationSyntax)ctx.Node
        ).Where(m => m is not null);

        var compilation = context.CompilationProvider.Combine(provider.Collect());

        context.RegisterSourceOutput(compilation, Execute);

    }

    private void Execute(SourceProductionContext context, (Compilation Left, ImmutableArray<ClassDeclarationSyntax> Right) arg2)
    {
        var (compilation, list) = arg2;

        var nameList = new List<string>();

        foreach (var syntax in list)
        {
            if (compilation.GetSemanticModel(syntax.SyntaxTree)
                    .GetDeclaredSymbol(syntax) is INamedTypeSymbol symbol)
            {
                nameList.Add(symbol.Name);
            }
        }



        var theCode = """
                      adsljf;lk
                      """;
        context.AddSource("Hrmmmmm.g.cs", theCode);
    }
}
