using NexusMods.MnemonicDB.Abstractions.ElementComparers;

namespace NexusMods.MnemonicDB.Abstractions.Attributes;

/// <summary>
/// An attribute that represents a string.
/// </summary>
public class StringAttribute(string ns, string name) : ScalarAttribute<string, string>(ValueTags.Utf8, ns, name)
{
    /// <inheritdoc />
    protected override string ToLowLevel(string value) => value;

    /// <inheritdoc />
    protected override string FromLowLevel(string lowLevelType, ValueTags tags) => lowLevelType;
}
