using System;
using Microsoft.CodeAnalysis;

namespace NexusMods.MnemonicDB.SourceGenerator;

public record MethodChain()
{
    public string Namespace { get; set; } = "";
    public MethodCall[] Methods { get; set; } = [];

    public ConcreteModel Analyze()
    {
        var model = new ConcreteModel();

        foreach (var method in Methods)
        {
            switch (method.MethodName)
            {
                case "New":
                    model.Name = method.Arguments[0].Value.ToString();
                    model.FullName = $"{Namespace}.{model.Name}";
                    model.Namespace = Namespace;
                    break;
                case "WithAttribute":
                {
                    var attribute = new ConcreteAttribute
                    {
                        Name = method.Arguments[0].Value.ToString(),
                        Type = method.GenericTypes![0]
                    };
                    FindAttributeInInheritanceTree((INamedTypeSymbol)attribute.Type);
                    model.Attributes.Add(attribute);
                    break;
                }
            }
        }
        return model;
    }

    public (ITypeSymbol, ITypeSymbol)? FindAttributeInInheritanceTree(INamedTypeSymbol typeSymbol)
    {
        while (typeSymbol != null)
        {
            if (typeSymbol.OriginalDefinition.ToDisplayString() == "Attribute<TValueType, TLowLevelType>")
            {
                return (typeSymbol.TypeArguments[0], typeSymbol.TypeArguments[1]);
            }

            typeSymbol = typeSymbol.BaseType!;
        }

        return null;

    }
}
