using JetBrains.Annotations;
using NexusMods.MnemonicDB.Abstractions.ValueSerializers;

namespace NexusMods.MnemonicDB.Abstractions.Attributes;

/// <summary>
/// An attribute that encodes a symbol.
/// </summary>
[PublicAPI]
public sealed class SymbolAttribute(string ns, string name) : ScalarAttribute<Symbol, string, AsciiSerializer>(ns, name)
{
    /// <inheritdoc />
    public override string ToLowLevel(Symbol value) => value.Id;

    /// <inheritdoc />
    public override Symbol FromLowLevel(string value, AttributeResolver resolver) => Symbol.InternPreSanitized(value);
}

