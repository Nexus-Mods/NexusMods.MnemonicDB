using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace NexusMods.MnemonicDB.SourceGenerator;

public class AnalyzedAttribute
{
    public string Name { get; set; } = "";
    public INamedTypeSymbol AttributeType { get; set; } = null!;
    public INamedTypeSymbol HighLevelType { get; set; } = null!;
    public INamedTypeSymbol LowLevelType { get; set; } = null!;
    public HashSet<string> Markers { get; set; } = new();
    public string Comments { get; set; } = "";
}
