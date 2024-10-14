using NexusMods.MnemonicDB.Abstractions.ElementComparers;

namespace NexusMods.MnemonicDB.Abstractions.Attributes;

/// <summary>
/// UInt64 attribute (ulong)
/// </summary>
public sealed class ULongAttribute(string ns, string name) : ScalarAttribute<ulong, ulong>(ValueTag.UInt64, ns, name)
{
    /// <inheritdoc />
    protected override ulong ToLowLevel(ulong value) => value;

    /// <inheritdoc />
    protected override ulong FromLowLevel(ulong value, AttributeResolver resolver) => value;
}
