using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace NexusMods.MnemonicDB.SourceGenerator;

[Generator(LanguageNames.CSharp)]
public class SqlResultGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var records = context.SyntaxProvider.ForAttributeWithMetadataName(
            fullyQualifiedMetadataName: "NexusMods.HyperDuck.QueryResultAttribute",
            predicate: (node, _) => FilterSyntaxNode(node),
            transform: TransformSyntaxContext
        ).Where(static x => x.HasValue);

        context.RegisterSourceOutput(records, (productionContext, data) => Execute(productionContext, data!.Value));
    }

    private static void Execute(SourceProductionContext productionContext, ResultData data)
    {
        var writer = new System.IO.StringWriter();
        Templates.Render(data, writer);

        productionContext.AddSource(hintName: $"QueryDataResult.{data.Symbol.Name}.g.cs", SourceText.From(writer.ToString(), Encoding.UTF8));
    }

    private static bool FilterSyntaxNode(SyntaxNode node) => node is RecordDeclarationSyntax;

    private static ResultData? TransformSyntaxContext(GeneratorAttributeSyntaxContext syntaxContext, CancellationToken cancellationToken)
    {
        if (syntaxContext.TargetNode is not RecordDeclarationSyntax declarationSyntax) return null;
        if (syntaxContext.SemanticModel.GetDeclaredSymbol(declarationSyntax, cancellationToken) is not INamedTypeSymbol recordSymbol) return null;

        var parameterList = declarationSyntax.ParameterList;
        if (parameterList is null) return null;

        var parameters = new ParameterData[parameterList.Parameters.Count];
        for (var i = 0; i < parameterList.Parameters.Count; i++)
        {
            var parameter = parameterList.Parameters[0];
            var identifier = parameter.Identifier;

            var type = parameter.Type;
            if (type is null) return null;

            var symbolInfo = syntaxContext.SemanticModel.GetSymbolInfo(type, cancellationToken);

            var symbol = symbolInfo.Symbol;
            if (symbol is null) return null;

            parameters[i] = new ParameterData(i, identifier, symbol, symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
        }

        return new ResultData(recordSymbol, parameters, recordSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
    }

    internal record struct ResultData(INamedTypeSymbol Symbol, ParameterData[] Parameters, string DisplayString);

    internal record struct ParameterData(int Index, SyntaxToken Identifier, ISymbol Symbol, string DisplayString);
}
