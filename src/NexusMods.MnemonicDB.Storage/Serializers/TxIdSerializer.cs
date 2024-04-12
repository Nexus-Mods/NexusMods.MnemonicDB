using System;
using System.Buffers;
using System.Diagnostics;
using System.Runtime.InteropServices;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Internals;
using NexusMods.MnemonicDB.Abstractions.Serializers;
using Reloaded.Memory.Extensions;

namespace NexusMods.MnemonicDB.Storage.Serializers;

internal class TxIdSerializer() : AUnmanagedSerializer<TxId, ulong>(LowLevelTypes.UInt, sizeof(ulong))
{
    protected override ulong ToLowLevel(TxId src) => src.Value;

    protected override TxId FromLowLevel(ulong src) => TxId.From(src);

    public override Symbol UniqueId => Symbol.Intern<TxIdSerializer>();

}
