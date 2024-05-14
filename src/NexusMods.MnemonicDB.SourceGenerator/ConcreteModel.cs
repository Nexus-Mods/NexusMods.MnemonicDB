using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace NexusMods.MnemonicDB.SourceGenerator;

public record ConcreteModel
{
    public string FullName { get; set; } = "";
    public string Name { get; set; } = "";
    public string Namespace { get; set; } = "";

    public List<ConcreteAttribute> Attributes { get; set; } = new();

    public List<ReferenceAttribute> References { get; set; } = new();

    public List<Include> Includes { get; set; } = new();

    public List<BackRef> BackRefs { get; set; } = new();
}


public record ConcreteAttribute
{
    public string Name { get; set; } = "";
    public ITypeSymbol Type { get; set; } = null!;

    public string HighLevelType => TypeInfo!.HighLevel.ToDisplayString();

    public AttributeTypeInfo? TypeInfo { get; set; } = null!;

    public bool IsIndexed { get; set; } = false;

    public bool NoHistory { get; set; } = false;

    public string AttributePostfix
    {
        get
        {
            List<string> postfixes = new();
            if (IsIndexed)
                postfixes.Add("IsIndexed = true");
            if (NoHistory)
                postfixes.Add("NoHistory = true");
            if (postfixes.Count == 0)
                return "";
            return $"{{ {string.Join(", ", postfixes)} }}";
        }
    }

    public string PrivateMemberName => "_" + Name[0].ToString().ToLower() + Name[1..];
}

public record Include
{
    public ITypeSymbol TypeInfo { get; set; } = default!;
}

public record BackRef
{
    public ITypeSymbol OtherModel { get; set; } = default!;

    public string OtherModelName => OtherModel.ToDisplayString();

    public string OtherAttributeName { get; set; } = "";

    public string ThisAttributeName { get; set; } = "";
}

public record ReferenceAttribute
{
    public string Name { get; set; } = "";

    public string AttributeName
    {
        get
        {
            if (MultiCardinality) return Name + "Ids";
            return Name + "Id";
        }
    }

    /// <summary>
    /// If this is a reference, this is the model that it references.
    /// </summary>
    public ITypeSymbol ReferenceModel { get; set; } = null!;

    public string ReferenceModelName => ReferenceModel.ToDisplayString();

    public bool MultiCardinality { get; set; } = false;
}


public record AttributeTypeInfo
{
    public INamedTypeSymbol HighLevel { get; set; } = null!;
    public INamedTypeSymbol LowLevel { get; set; } = null!;
}
