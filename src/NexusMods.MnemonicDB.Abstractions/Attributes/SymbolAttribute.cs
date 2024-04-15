using NexusMods.MnemonicDB.Abstractions.ElementComparers;

namespace NexusMods.MnemonicDB.Abstractions.Attributes;

/// <summary>
/// An attribute that encodes a symbol.
/// </summary>
public class SymbolAttribute(string ns, string name) : ScalarAttribute<Symbol, string>(ValueTags.Ascii, ns, name)
{
    /// <inheritdoc />
    protected override string ToLowLevel(Symbol value) => value.Id;

    /// <inheritdoc />
    protected override Symbol FromLowLevel(string lowLevelType, ValueTags tags) => Symbol.InternPreSanitized(lowLevelType);
}

