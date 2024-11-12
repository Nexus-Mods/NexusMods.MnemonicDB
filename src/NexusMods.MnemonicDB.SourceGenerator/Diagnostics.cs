using Microsoft.CodeAnalysis;

namespace NexusMods.MnemonicDB.SourceGenerator;

internal static class Diagnostics
{
    internal static readonly DiagnosticDescriptor InvalidModelClassDefinition = new(
        id: "NMD001",
        title: "Invalid Model Class Definition",
        messageFormat: "Model {0} must be non-static, partial, and implement IModelDefinition",
        category: "Usage",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);
    
    internal static readonly DiagnosticDescriptor ReferenceAttributeMustDefineReferencedModel = new(
        id: "NMD002",
        title: "Reference attribute must declare referenced model",
        messageFormat: "The attribute {0}.{1} does not declare a referenced model. All reference attributes must provide a reference model via a generic type parameter, use ReferenceAttribute<TModel> instead.",
        category: "Usage",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);
}
