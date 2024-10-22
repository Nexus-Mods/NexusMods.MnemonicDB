using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NexusMods.MnemonicDB.SourceGenerator;

public class ModelAnalyzer
{
    private readonly Compilation _compilation;
    private readonly GeneratorExecutionContext _context;
    private readonly ClassDeclarationSyntax _syntax;
    private readonly INamedTypeSymbol _classSymbol;
    private readonly INamedTypeSymbol _attributeTypeSymbol;
    private readonly INamedTypeSymbol _includesTypeSymbol;
    private readonly INamedTypeSymbol _modelDefinitionTypeSymbol;
    private readonly INamedTypeSymbol _backReferenceTypeSymbol;
    private readonly INamedTypeSymbol _markerAttributeTypeSymbol;
    private readonly INamedTypeSymbol _scalarAttributeTypeSymbol;
    private readonly INamedTypeSymbol _collectionAttributeTypeSymbol;
    private readonly INamedTypeSymbol _entityIdTypeSymbol;
    private readonly INamedTypeSymbol _referenceAttributeTypeSymbol;
    private readonly INamedTypeSymbol _referencesAttributeTypeSymbol;

    #region OutputProperties

    public string Name { get; set; } = "";
    public INamespaceSymbol Namespace { get; set; } = null!;

    public List<AnalyzedAttribute> Attributes { get; } = new();
    
    public List<AnalyzedBackReferenceAttribute> BackReferences { get; } = new();

    public List<INamedTypeSymbol> Includes { get; set; } = new();
    public string Comments { get; set; } = "";

    #endregion

    public ModelAnalyzer(ClassDeclarationSyntax declarationSyntax, GeneratorExecutionContext context)
    {
        _context = context;
        _syntax = declarationSyntax;
        _compilation = context.Compilation;
        _classSymbol = (INamedTypeSymbol?)ModelExtensions.GetDeclaredSymbol(_compilation.GetSemanticModel(declarationSyntax.SyntaxTree), declarationSyntax)!;
        _modelDefinitionTypeSymbol = _compilation.GetTypeByMetadataName(Consts.IModelDefinitionFullName)!;
        _attributeTypeSymbol = _compilation.GetTypeByMetadataName(Consts.AttributeTypeFullName)!;
        _includesTypeSymbol = _compilation.GetTypeByMetadataName(Consts.IncludesAttributeFullName)!;
        _backReferenceTypeSymbol = _compilation.GetTypeByMetadataName(Consts.BackReferenceAttributeFullName)!;
        _markerAttributeTypeSymbol = _compilation.GetTypeByMetadataName(Consts.MarkerAttributeFullName)!;
        _scalarAttributeTypeSymbol = _compilation.GetTypeByMetadataName(Consts.ScalarAttributeFullName)!;
        _collectionAttributeTypeSymbol = _compilation.GetTypeByMetadataName(Consts.CollectionAttributeFullName)!;
        _referenceAttributeTypeSymbol = _compilation.GetTypeByMetadataName(Consts.ReferenceAttributeFullName)!;
        _referencesAttributeTypeSymbol = _compilation.GetTypeByMetadataName(Consts.ReferecnesAttributeFullName)!;
        _entityIdTypeSymbol = _compilation.GetTypeByMetadataName(Consts.EntityIdFullName)!;
    }

    public bool Analyze()
    {
        if (!InheritsFromModelDefinition())
            return false;

        Name = _classSymbol.Name;
        Namespace = _classSymbol.ContainingNamespace;

        AnalyzeAttributes();
        Includes = AnalyzeIncludes();
        Comments = AnalyzeComments();

        return true;

    }


    private string AnalyzeComments()
    {
        var triviaList = _syntax.SyntaxTree.GetRoot().DescendantTrivia()
            .Where(trivia => trivia.Span.End <= _syntax.SpanStart &&
                             (trivia.IsKind(SyntaxKind.SingleLineCommentTrivia) ||
                              trivia.IsKind(SyntaxKind.MultiLineCommentTrivia)));

        return string.Join("\n", triviaList.Select(comment => comment.ToString()));
    }

    private List<INamedTypeSymbol> AnalyzeIncludes()
    {
        var includes = new List<INamedTypeSymbol>();

        foreach (var attributeList in _syntax.AttributeLists)
        {
            foreach (var attribute in attributeList.Attributes)
            {
                var attributeSymbol = ModelExtensions.GetSymbolInfo(_compilation.GetSemanticModel(attribute.SyntaxTree), attribute).Symbol as IMethodSymbol;

                if (attributeSymbol != null && SymbolEqualityComparer.Default.Equals(attributeSymbol.ContainingType.OriginalDefinition, _includesTypeSymbol))
                {
                    var typeArgumentSyntax = ((GenericNameSyntax)attribute.Name).TypeArgumentList.Arguments[0];
                    var typeSymbol = ModelExtensions.GetTypeInfo(_compilation.GetSemanticModel(typeArgumentSyntax.SyntaxTree), typeArgumentSyntax).Type;

                    if (typeSymbol != null && typeSymbol.AllInterfaces.Any(i => SymbolEqualityComparer.Default.Equals(i, _modelDefinitionTypeSymbol)))
                    {
                        includes.Add((INamedTypeSymbol)typeSymbol);
                    }
                }
            }
        }

        return includes;
    }

    private void AnalyzeAttributes()
    {
        foreach (var member in _classSymbol.GetMembers())
        {
            if (member is not IFieldSymbol fieldSymbol)
                continue;

            if (TryGetBackReference(fieldSymbol, out var otherModel, out var otherAttribute))
            {
                var comments = GetComments(fieldSymbol);
                var analyzedAttribute = new AnalyzedBackReferenceAttribute
                {
                    Name = fieldSymbol.Name,
                    OtherModel = otherModel!,
                    OtherAttribute = otherAttribute!,
                    Comments = comments
                };
                BackReferences.Add(analyzedAttribute);
            }
            else if (TryGetAttributeTypes(fieldSymbol, out var highLevel, out var lowLevel, out var serializer, out var flags))
            {
                var markers = GetInitializerData(fieldSymbol);
                var comments = GetComments(fieldSymbol);
                var analyzedAttribute = new AnalyzedAttribute
                {
                    Name = fieldSymbol.Name,
                    FieldName = fieldSymbol.ToDisplayString(),
                    Flags = flags,
                    AttributeType = (fieldSymbol.Type as INamedTypeSymbol)!,
                    HighLevelType = highLevel!,
                    LowLevelType = lowLevel!,
                    SerializerType = serializer!,
                    Markers = markers,
                    Comments = comments
                };

                if (SymbolEqualityComparer.Default.Equals(fieldSymbol.Type.OriginalDefinition, _referenceAttributeTypeSymbol))
                {
                    analyzedAttribute.ReferenceType = ((fieldSymbol.Type as INamedTypeSymbol)!.TypeArguments[0] as INamedTypeSymbol)!;
                }

                if (SymbolEqualityComparer.Default.Equals(fieldSymbol.Type.OriginalDefinition, _referencesAttributeTypeSymbol))
                {
                    analyzedAttribute.ReferenceType = ((fieldSymbol.Type as INamedTypeSymbol)!.TypeArguments[0] as INamedTypeSymbol)!;
                }

                Attributes.Add(analyzedAttribute);
            }
        }
    }

    private bool TryGetBackReference(IFieldSymbol fieldSymbol, out INamedTypeSymbol? otherModel, out IFieldSymbol? otherAttribute)
    {
        otherModel = null;
        otherAttribute = null;

        if (SymbolEqualityComparer.Default.Equals(fieldSymbol.Type.OriginalDefinition, _backReferenceTypeSymbol))
        {
            var namedTypeSymbol = (INamedTypeSymbol)fieldSymbol.Type;
            otherModel = (INamedTypeSymbol)namedTypeSymbol.TypeArguments[0]; // This will give you the other model

            var syntaxReference = fieldSymbol.DeclaringSyntaxReferences[0];
            var syntax = (VariableDeclaratorSyntax)syntaxReference.GetSyntax();

            var objectCreationExpression = (ImplicitObjectCreationExpressionSyntax)syntax.Initializer!.Value;
            var argumentExpression = objectCreationExpression.ArgumentList!.Arguments[0].Expression;

            var semanticModel = _compilation.GetSemanticModel(argumentExpression.SyntaxTree);
            var symbolInfo = semanticModel.GetSymbolInfo(argumentExpression);
            otherAttribute = (IFieldSymbol)symbolInfo.Symbol!; // This is the other attribute

            return true;
        }

        return false;
    }


    private string GetComments(IFieldSymbol fieldSymbol)
{
    if (fieldSymbol.DeclaringSyntaxReferences.Length > 0)
    {
        var syntaxReference = fieldSymbol.DeclaringSyntaxReferences[0];
        var syntax = (VariableDeclaratorSyntax)syntaxReference.GetSyntax();

        var triviaList = syntax.SyntaxTree.GetRoot().DescendantTrivia()
            .Where(trivia => trivia.Span.End <= syntax.SpanStart &&
                             (trivia.IsKind(SyntaxKind.SingleLineCommentTrivia) ||
                              trivia.IsKind(SyntaxKind.MultiLineCommentTrivia)));

        return string.Join("\n", triviaList.Select(comment => comment.ToString()));
    }

    return string.Empty;
}

    private HashSet<string> GetInitializerData(IFieldSymbol fieldSymbol)
    {
        var markers = new HashSet<string>();

        if (fieldSymbol.DeclaringSyntaxReferences.Length > 0)
        {
            var syntaxReference = fieldSymbol.DeclaringSyntaxReferences[0];
            var syntax = syntaxReference.GetSyntax();
            if (syntax is not VariableDeclaratorSyntax variableDeclaratorSyntax)
                return markers;

            var initializerExpression = variableDeclaratorSyntax.Initializer?.Value;

            if (initializerExpression is not ImplicitObjectCreationExpressionSyntax objectCreation)
                return markers;

            var initializer = objectCreation.Initializer;

            if (initializer == null)
                return markers;

            foreach (var expression in initializer.Expressions)
            {
                if (expression is AssignmentExpressionSyntax {
                        Left: IdentifierNameSyntax identifier,
                        Right: LiteralExpressionSyntax
                    {
                        Token.ValueText: "true"
                    }})
                {
                    markers.Add(identifier.Identifier.Text);
                }
            }
        }
        return markers;
    }

    private bool TryGetAttributeTypes(IFieldSymbol fieldSymbol,
        out ITypeSymbol? highLevel,
        out ITypeSymbol? lowLevel,
        out ITypeSymbol? serializer,
        out AttributeFlags flags)
    {
        var type = fieldSymbol.Type;
        highLevel = null;
        lowLevel = null;
        serializer = null;
        flags = 0;

        while (true)
        {
            if (type is INamedTypeSymbol namedTypeSymbol)
            {
                if (SymbolEqualityComparer.Default.Equals(type.OriginalDefinition, _scalarAttributeTypeSymbol))
                    flags |= AttributeFlags.Scalar;

                if (SymbolEqualityComparer.Default.Equals(type.OriginalDefinition, _collectionAttributeTypeSymbol))
                    flags |= AttributeFlags.Collection;

                if (SymbolEqualityComparer.Default.Equals(type.OriginalDefinition, _markerAttributeTypeSymbol))
                    flags |= AttributeFlags.Marker;

                if (SymbolEqualityComparer.Default.Equals(type.OriginalDefinition, _attributeTypeSymbol))
                {
                    Debug.Assert(namedTypeSymbol.TypeArguments.Length == 3);

                    highLevel = namedTypeSymbol.TypeArguments[0];
                    if (SymbolEqualityComparer.Default.Equals(highLevel, _entityIdTypeSymbol))
                        flags |= AttributeFlags.Reference;

                    lowLevel = namedTypeSymbol.TypeArguments[1];
                    
                    serializer = namedTypeSymbol.TypeArguments[2];
                    
                    return true;
                }

                type = namedTypeSymbol.BaseType;
            }
            else
            {
                return false;
            }
        }
    }


    private bool InheritsFromModelDefinition()
    {
        foreach (var i in _classSymbol.Interfaces)
        {
            if (SymbolEqualityComparer.Default.Equals(i, _modelDefinitionTypeSymbol))
                return true;
        }
        return false;
    }
}
