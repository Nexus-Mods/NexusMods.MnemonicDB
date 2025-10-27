using NexusMods.Hashing.xxHash3;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.ValueSerializers;

namespace NexusMods.MnemonicDB.TestModel.Attributes;

public sealed class HashAttribute(string ns, string name) : ScalarAttribute<Hash, ulong, UInt64Serializer>(ns, name)
{
    public override ulong ToLowLevel(Hash value) => value.Value;

    public override Hash FromLowLevel(ulong value, AttributeResolver resolver) => Hash.From(value);
}
