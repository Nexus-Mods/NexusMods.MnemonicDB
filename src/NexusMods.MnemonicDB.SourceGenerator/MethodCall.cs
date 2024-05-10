using System.Collections.Generic;

namespace NexusMods.MnemonicDB.SourceGenerator;

public record MethodCall()
{
    public string MethodName { get; set; } = "";
    public List<string>? GenericTypes { get; set; } = new();
    public List<object> Arguments { get; set; } = new();
}
