using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Internals;
using NexusMods.MnemonicDB.Abstractions.Serializers;

namespace NexusMods.MnemonicDB.Storage.Serializers;

internal class UInt64Serializer() : AUnmanagedSerializer<ulong, ulong>(LowLevelTypes.UInt, sizeof(ulong))
{
    protected override ulong ToLowLevel(ulong src) => src;

    protected override ulong FromLowLevel(ulong src) => src;

    public override Symbol UniqueId => Symbol.Intern<UInt64Serializer>();
}
