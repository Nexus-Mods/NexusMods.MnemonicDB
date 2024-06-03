using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NexusMods.MnemonicDB.SourceGenerator;

public class SyntaxReceiver : ISyntaxReceiver
{
    public ImmutableList<ClassDeclarationSyntax> CandidateClasses { get; private set; } =
        ImmutableList<ClassDeclarationSyntax>.Empty;

    public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
    {
        if (syntaxNode is not ClassDeclarationSyntax)
            return;



        var classDeclarationSyntax = (ClassDeclarationSyntax)syntaxNode;
        var implementedInterfaces = classDeclarationSyntax.BaseList?.Types;

        if (implementedInterfaces is null)
            return;

        if (implementedInterfaces.All<BaseTypeSyntax>(x => x.ToString() != Consts.IModelDefinitionName))
            return;

        CandidateClasses = CandidateClasses.Add(classDeclarationSyntax);
    }
}
