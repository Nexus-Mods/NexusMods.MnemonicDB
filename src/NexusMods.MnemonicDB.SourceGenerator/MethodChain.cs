using System;
using System.Linq;
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
                case "Attribute":
                {
                    var attribute = new ConcreteAttribute
                    {
                        Name = method.Arguments[0].Value.ToString(),
                        Type = method.GenericTypes![0],
                        TypeInfo = FindAttributeInInheritanceTree((INamedTypeSymbol)method.GenericTypes![0])
                    };

                    if (method.Arguments.Any(a => a.Key == "isIndexed" && a.Value is true))
                        attribute.IsIndexed = true;

                    if (method.Arguments.Any(a => a.Key == "noHistory" && a.Value is true))
                        attribute.NoHistory = true;

                    model.Attributes.Add(attribute);
                    break;
                }
                case "Reference":
                {
                    var attribute = new ReferenceAttribute
                    {
                        Name = method.Arguments[0].Value.ToString(),
                        ReferenceModel = method.GenericTypes![0]
                    };

                    model.References.Add(attribute);
                    break;
                }
                case "References":
                {
                    var attribute = new ReferenceAttribute
                    {
                        Name = method.Arguments[0].Value.ToString(),
                        ReferenceModel = method.GenericTypes![0],
                        MultiCardinality = true
                    };

                    model.References.Add(attribute);
                    break;
                }
            }
        }
        return model;
    }

    public AttributeTypeInfo? FindAttributeInInheritanceTree(INamedTypeSymbol? typeSymbol)
    {
        while (typeSymbol != null)
        {
            if (typeSymbol.OriginalDefinition.ToDisplayString() == "NexusMods.MnemonicDB.Abstractions.Attribute<TValueType, TLowLevelType>")
            {
                return new AttributeTypeInfo
                {
                    HighLevel = (INamedTypeSymbol)typeSymbol.TypeArguments[0],
                    LowLevel = (typeSymbol.TypeArguments[1] as INamedTypeSymbol)!
                };
            }

            typeSymbol = typeSymbol.BaseType!;
        }

        return null;

    }
}
