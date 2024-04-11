using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Hashing.xxHash64;
using NexusMods.MnemonicDB.Abstractions.Internals;
using NexusMods.MnemonicDB.Abstractions.Serializers;

namespace NexusMods.MnemonicDB.TestModel.ValueSerializers;

public class HashSerializer() : AUnmanagedSerializer<Hash>(LowLevelTypes.UInt, sizeof(ulong))
{
    public override Symbol UniqueId => Symbol.Intern<HashSerializer>();
}
