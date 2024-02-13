using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NexusMods.EventSourcing.SourceGenerator;

[Generator]
public class ModelGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var provider = context.SyntaxProvider.CreateSyntaxProvider(
                predicate: Predicate,
                transform: Transform)
            .Where(cls => cls != null);

        var compilation = context.CompilationProvider.Combine(provider.Collect());


        context.RegisterSourceOutput(compilation, (spc, source) => Execute(spc, source.Left, source.Right!));
    }

    private bool Predicate(SyntaxNode node, CancellationToken _)
    {
        if (node is not ClassDeclarationSyntax syntax) return false;

        foreach (var attributeList in syntax.AttributeLists)
        {
            foreach (var attr in attributeList.Attributes)
            {
                if (attr.Name.ToString() == "ModelDefinition")
                {
                    return true;
                }
            }
        }

        return false;
    }

    private ClassDeclarationSyntax? Transform(GeneratorSyntaxContext syntaxContext, CancellationToken arg2)
    {
        if (syntaxContext.Node is ClassDeclarationSyntax classDeclarationSyntax)
            return classDeclarationSyntax;
        return null;
    }

    private void Execute(SourceProductionContext context, Compilation compilation,
        ImmutableArray<ClassDeclarationSyntax> modelDefinitionLists)
    {

        var attributeData = FindAttributes(compilation, modelDefinitionLists);
        var source = GenerateSource(attributeData);
        context.AddSource("NexusMods.EventSourcing.Attributes.g.cs", source);
    }

    private string GenerateSource(IEnumerable<AttributeData> attributeData)
    {
        var grouped = attributeData.GroupBy(a => new { a.Namespace, a.Entity })
            .Select(group => new AttributeGroup
            {
                Namespace = group.Key.Namespace,
                Entity = group.Key.Entity,
                Attributes = group
            });

        var sb = new CodeWriter();
        sb.Line("using System;");
        sb.Line("using NexusMods.EventSourcing.Abstractions;");
        sb.Line("using Microsoft.Extensions.DependencyInjection;");

        foreach (var group in grouped)
        {
            sb.Line("namespace " + group.Namespace);
            sb.Line("{");

            sb.Line("public static partial class " + group.Entity);
            sb.Line("{");

            foreach (var attribute in group.Attributes)
            {
                sb.ClassComment(attribute.Description);
                var withoutQuotes = attribute.Name.Replace("\"", "");
                var uniqueName = $"{attribute.Namespace}.{attribute.Entity}/{withoutQuotes}";
                sb.Line($"public class {withoutQuotes}() : ScalarAttribute<{withoutQuotes}, {attribute.AttributeType}>(\"{uniqueName}\");");
                sb.BlankLine();
            }

            sb.BlankLine();
            sb.ClassComment("Add the attributes to the service collection");
            sb.Line("internal static IServiceCollection Add" + group.Entity + "Model(this IServiceCollection services)");
            sb.Line("{");

            foreach (var attribute in group.Attributes)
            {
                var withoutQuotes = attribute.Name.Replace("\"", "");
                sb.Line($"services.AddAttribute<{withoutQuotes}>();");
            }
            sb.Line("return services;");
            sb.Line("}");

            sb.Line("}");
            sb.Line("}");

        }

        var source = sb.ToString();

        return source;
    }

    private static IEnumerable<AttributeData> FindAttributes(Compilation compilation,
        ImmutableArray<ClassDeclarationSyntax> modelDefinitionLists)
    {
        var foundAttributes = new List<AttributeData>();
        foreach (var modelDefinition in modelDefinitionLists)
        {
            var semanticModel = compilation.GetSemanticModel(modelDefinition.SyntaxTree);
            var declaredSymbol = semanticModel.GetDeclaredSymbol(modelDefinition);

            foreach (var member in modelDefinition.Members)
            {
                if (member is FieldDeclarationSyntax fieldDeclaration)
                {
                    var fieldType = fieldDeclaration.Declaration.Type;
                    var fieldSymbol = semanticModel.GetSymbolInfo(fieldType).Symbol;

                    if (fieldSymbol?.ToString() ==
                        "NexusMods.EventSourcing.Abstractions.ModelGeneration.AttributeDefinitions")
                    {
                        var initializer = fieldDeclaration.Declaration.Variables.First().Initializer;
                        if (initializer != null)
                        {
                            var builderExpression = initializer.Value as InvocationExpressionSyntax;
                            if (builderExpression != null)
                            {
                                var defineCalls = builderExpression.DescendantNodesAndSelf()
                                    .OfType<InvocationExpressionSyntax>()
                                    .Where(invocation => invocation.Expression is MemberAccessExpressionSyntax
                                    {
                                        Name.Identifier.Text: "Define"
                                    });

                                foreach (var defineCall in defineCalls)
                                {
                                    var genericDefineCall =
                                        ((MemberAccessExpressionSyntax)defineCall.Expression).Name as GenericNameSyntax;
                                    if (genericDefineCall == null)
                                    {
                                        continue;
                                    }

                                    var typeArgument = genericDefineCall.TypeArgumentList.Arguments.First();
                                    var typeSymbol = semanticModel.GetSymbolInfo(typeArgument).Symbol;
                                    Console.WriteLine($"Type: {typeSymbol}");


                                    var args = defineCall.ArgumentList.Arguments.Select(argument =>
                                    {
                                        var name = argument.Expression as LiteralExpressionSyntax;

                                        return name;
                                    }).ToArray();

                                    foundAttributes.Add(new AttributeData
                                    {
                                        Name = args[0]!.Token.ValueText,
                                        Entity = declaredSymbol!.Name,
                                        AttributeType = typeSymbol?.ToString() ?? "",
                                        Description = args[1]!.Token.ValueText,
                                        Namespace = declaredSymbol?.ContainingNamespace.ToString() ?? ""
                                    });
                                }
                            }
                        }
                    }
                }
            }
        }

        return foundAttributes;
    }
}
