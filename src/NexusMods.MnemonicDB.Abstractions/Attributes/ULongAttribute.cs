using NexusMods.MnemonicDB.Abstractions.ElementComparers;

namespace NexusMods.MnemonicDB.Abstractions.Attributes;

/// <summary>
/// UInt64 attribute (ulong)
/// </summary>
public class ULongAttribute(string ns, string name) : ScalarAttribute<ulong, ulong>(ValueTags.UInt64, ns, name)
{
    /// <inheritdoc />
    protected override ulong ToLowLevel(ulong value)
    {
        return value;
    }

    /// <inheritdoc />
    protected override ulong FromLowLevel(ulong value, ValueTags tags, AttributeResolver resolver)
    {
        return value;
    }
}
