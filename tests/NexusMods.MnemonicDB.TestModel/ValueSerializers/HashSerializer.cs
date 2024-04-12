using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Hashing.xxHash64;
using NexusMods.MnemonicDB.Abstractions.Internals;
using NexusMods.MnemonicDB.Abstractions.Serializers;

namespace NexusMods.MnemonicDB.TestModel.ValueSerializers;

public class HashSerializer() : AUnmanagedSerializer<Hash, ulong>(LowLevelTypes.UInt, sizeof(ulong))
{
    protected override ulong ToLowLevel(Hash src) => src.Value;

    protected override Hash FromLowLevel(ulong src) => Hash.From(src);

    public override Symbol UniqueId => Symbol.Intern<HashSerializer>();
}
