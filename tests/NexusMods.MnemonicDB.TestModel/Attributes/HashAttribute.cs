using NexusMods.Hashing.xxHash64;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;

namespace NexusMods.MnemonicDB.TestModel.Attributes;

public class HashAttribute(string ns, string name) : ScalarAttribute<Hash, ulong>(ValueTags.UInt64, ns, name)
{
    protected override ulong ToLowLevel(Hash value) => value.Value;
    protected override Hash FromLowLevel(ulong lowLevelType, ValueTags tags) => Hash.From(lowLevelType);
}
