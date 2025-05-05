﻿using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace NexusMods.MnemonicDB.SourceGenerator;

internal record AnalyzedAttribute
{
    public string Name { get; set; } = "";
    public string FieldName { get; set; } = "";
    public string Cref { get; set; } = string.Empty;

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
    public bool IsOptional => Markers.Contains("IsOptional");

    /// <summary>
    /// True if this is a value type.
    /// </summary>
    public bool IsValueType => HighLevelType.IsValueType;

    /// <summary>
    /// The C# prefix for the attribute, for now this is just required or empty
    /// </summary>
    public string Prefix => IsOptional ? "" : "required";

    /// <summary>
    /// The part of the attribute definition that comes after the type
    /// </summary>
    public string Postfix => IsOptional ? "?" : "";

    /// <summary>
    /// True if this is a marker attribute.
    /// </summary>
    public bool IsMarker => Flags.HasFlag(AttributeFlags.Marker);

    public AttributeFlags Flags { get; set; }
    public INamedTypeSymbol AttributeType { get; set; } = null!;
    public ITypeSymbol HighLevelType { get; set; } = null!;
    public ITypeSymbol LowLevelType { get; set; } = null!;
    public INamedTypeSymbol ReferenceType { get; set; } = null!;
    public HashSet<string> Markers { get; set; } = new();
    public string Comments { get; set; } = "";
    public ITypeSymbol SerializerType { get; set; } = null!;
}
