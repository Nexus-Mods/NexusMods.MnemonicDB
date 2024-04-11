using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Internals;
using NexusMods.MnemonicDB.Abstractions.Serializers;
using NexusMods.Paths;

namespace NexusMods.MnemonicDB.TestModel.ValueSerializers;

public class SizeSerializer() : AUnmanagedSerializer<Size>(LowLevelTypes.UInt, sizeof(ulong))
{
    public override Symbol UniqueId => Symbol.Intern<SizeSerializer>();
}
