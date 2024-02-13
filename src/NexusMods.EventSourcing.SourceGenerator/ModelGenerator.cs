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
                sb.Line($"public class {withoutQuotes}() : ScalarAttribute<{withoutQuotes}, {attribute.AttributeType}>(\"{uniqueName}\")");
                sb.Line("{");
                sb.Line("public static void Assert(EntityId entityId, " + attribute.AttributeType + " value, ITransaction tx)");
                sb.Line("{");
                sb.Line("tx.Add(new AssertDatom<" + withoutQuotes + ", " + attribute.AttributeType + ">(entityId.Value, value));");
                sb.Line("}");
                sb.Line("}");
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
            sb.Line("services.AddReadModelFactory<" + group.Entity + "ReadModelFactory>();");
            sb.Line("return services;");
            sb.Line("}");

            sb.Line("}");

            EmitReadModel(group, sb);

            sb.Line("}");

        }

        var source = sb.ToString();

        return source;
    }

    private void EmitReadModel(AttributeGroup group, CodeWriter sb)
    {
        sb.BlankLine();
        sb.ClassComment("Read model for " + group.Entity);
        sb.Line("public class " + group.Entity + "ReadModel(EntityId id) : IReadModel");
        sb.Line("{");
        sb.BlankLine();

        sb.ClassComment("The entity id of the read model");
        sb.Line("public EntityId Id => id;");
        sb.BlankLine();

        foreach (var attribute in group.Attributes)
        {
            var withoutQuotes = attribute.Name.Replace("\"", "");
            sb.ClassComment(attribute.Description);
            sb.Line($"public {attribute.AttributeType} {withoutQuotes} {{get; private set; }} = default!;");
            sb.BlankLine();
        }

        sb.ClassComment("Read a datom into the read model");
        sb.Line("public void Set(IAttribute attribute, ReadOnlySpan<byte> value)");
        sb.Line("{");

        sb.Line("switch (attribute)");
        sb.Line("{");
        foreach (var attribute in group.Attributes)
        {
            var withoutQuotes = attribute.Name.Replace("\"", "");
            sb.Line($"case {group.Namespace}.{group.Entity}.{withoutQuotes} a:");
            sb.Line("{");
            sb.Line($"{withoutQuotes} = a.Read(value);");
            sb.Line("break;");
            sb.Line("}");
        }
        sb.Line("}");
        sb.Line("}");

        sb.Line("}");

        sb.BlankLine();

        sb.ClassComment("Read model factory " + group.Entity);
        sb.Line("public class " + group.Entity + "ReadModelFactory : IReadModelFactory");
        sb.Line("{");

        sb.ClassComment("The type of read model this factory creates");
        sb.Line("public Type ModelType => typeof(" + group.Entity + "ReadModel);");

        sb.ClassComment("Create a new read model instance for " + group.Entity);
        sb.Line("public IReadModel Create(EntityId id)");
        sb.Line("{");
        sb.Line("return new " + group.Entity + "ReadModel(id);");
        sb.Line("}");
        sb.BlankLine();

        sb.ClassComment("The attributes that are required for reading " + group.Entity);
        sb.Line("public Type[] Attributes => new Type[] {");
        foreach (var attribute in group.Attributes)
        {
            var withoutQuotes = attribute.Name.Replace("\"", "");
            sb.Line($"typeof({group.Namespace}.{group.Entity}.{withoutQuotes}),");
        }
        sb.Line("};");
        sb.BlankLine();

        sb.Line("}");
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
