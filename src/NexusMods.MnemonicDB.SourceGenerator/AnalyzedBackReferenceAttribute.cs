using System.Reflection;
using Microsoft.CodeAnalysis;

namespace NexusMods.MnemonicDB.SourceGenerator;

public class AnalyzedBackReferenceAttribute
{
    public string Name { get; set; } = "";
    public INamedTypeSymbol OtherModel { get; set; } = null!;
    public IFieldSymbol OtherAttribute { get; set; } = null!;
    public string Comments { get; set; } = "";
}
