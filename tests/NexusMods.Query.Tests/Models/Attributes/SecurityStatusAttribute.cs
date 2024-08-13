using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;

namespace NexusMods.Query.Tests.Models.Attributes;

public class SecurityStatusAttribute(string ns, string name) : ScalarAttribute<SecurityClass, byte>(ValueTags.UInt8, ns, name)
{
    protected override byte ToLowLevel(SecurityClass value)
    {
        return (byte)value;
    }

    protected override SecurityClass FromLowLevel(byte value, ValueTags tags, RegistryId registryId)
    {
        return (SecurityClass)value;
    }
}
