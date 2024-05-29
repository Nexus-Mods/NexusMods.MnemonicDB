using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace NexusMods.MnemonicDB.SourceGenerator;

public class AnalyzedAttribute
{
    public string Name { get; set; } = "";
    public string FieldName { get; set; } = "";

    public string ContextualName
    {
        get
        {
            if (Flags.HasFlag(AttributeFlags.Reference) && Flags.HasFlag(AttributeFlags.Scalar))
            {
                return $"{Name}Id";
            }
            if (Flags.HasFlag(AttributeFlags.Reference) && Flags.HasFlag(AttributeFlags.Collection))
            {
                return $"{Name}Ids";
            }
            return Name;
        }
    }

    public bool IsReference => Flags.HasFlag(AttributeFlags.Reference);
    public bool IsCollection => Flags.HasFlag(AttributeFlags.Collection);
    public bool IsScalar => Flags.HasFlag(AttributeFlags.Scalar);

    public bool IsIndexed => Markers.Contains("IsIndexed");

    /// <summary>
    /// True if this is a marker attribute.
    /// </summary>
    public bool IsMarker => Flags.HasFlag(AttributeFlags.Marker);

    public AttributeFlags Flags { get; set; }
    public INamedTypeSymbol AttributeType { get; set; } = null!;
    public INamedTypeSymbol HighLevelType { get; set; } = null!;
    public INamedTypeSymbol LowLevelType { get; set; } = null!;
    public INamedTypeSymbol ReferenceType { get; set; } = null!;
    public HashSet<string> Markers { get; set; } = new();
    public string Comments { get; set; } = "";
}
