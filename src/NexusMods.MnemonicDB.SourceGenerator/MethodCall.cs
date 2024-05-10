using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace NexusMods.MnemonicDB.SourceGenerator;

public record MethodCall()
{
    public string MethodName { get; set; } = "";
    public List<ITypeSymbol>? GenericTypes { get; set; } = new();
    public List<KeyValuePair<string, object>> Arguments { get; set; } = new();
}
