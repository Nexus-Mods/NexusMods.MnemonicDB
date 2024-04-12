using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Internals;
using NexusMods.MnemonicDB.Abstractions.Serializers;
using NexusMods.Paths;

namespace NexusMods.MnemonicDB.TestModel.ValueSerializers;

public class SizeSerializer() : AUnmanagedSerializer<Size, ulong>(LowLevelTypes.UInt, sizeof(ulong))
{
    protected override ulong ToLowLevel(Size src) => src.Value;

    protected override Size FromLowLevel(ulong src) => Size.From(src);

    public override Symbol UniqueId => Symbol.Intern<SizeSerializer>();
}
