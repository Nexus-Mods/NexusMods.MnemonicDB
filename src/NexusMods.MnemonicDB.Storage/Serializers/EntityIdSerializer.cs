using System;
using System.Buffers;
using System.Diagnostics;
using System.Runtime.InteropServices;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Internals;
using NexusMods.MnemonicDB.Abstractions.Serializers;
using Reloaded.Memory.Extensions;

namespace NexusMods.MnemonicDB.Storage.Serializers;

/// <inheritdoc />
public class EntityIdSerializer() : AUnmanagedSerializer<EntityId, ulong>(LowLevelTypes.UInt, sizeof(ulong))
{
    /// <inheritdoc />
    protected override ulong ToLowLevel(EntityId src) => src.Value;

    /// <inheritdoc />
    protected override EntityId FromLowLevel(ulong src) => EntityId.From(src);

    /// <inheritdoc />
    public override Symbol UniqueId => Symbol.Intern<EntityIdSerializer>();

}
