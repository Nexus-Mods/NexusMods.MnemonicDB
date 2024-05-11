using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace NexusMods.MnemonicDB.SourceGenerator;

public record ConcreteModel
{
    public string FullName { get; set; } = "";
    public string Name { get; set; } = "";
    public string Namespace { get; set; } = "";

    public List<ConcreteAttribute> Attributes { get; set; } = new();
}


public record ConcreteAttribute
{
    public string Name { get; set; } = "";
    public ITypeSymbol Type { get; set; } = null!;

    public AttributeTypeInfo? TypeInfo { get; set; } = null!;
}


public record AttributeTypeInfo
{
    public INamedTypeSymbol HighLevel { get; set; } = null!;
    public INamedTypeSymbol LowLevel { get; set; } = null!;
}
