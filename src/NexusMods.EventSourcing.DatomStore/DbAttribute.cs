using System;
using NexusMods.EventSourcing.Abstractions;

namespace NexusMods.EventSourcing.DatomStore;

public record DbAttribute(Symbol UniqueId, ulong AttrEntityId, UInt128 ValueTypeId);
