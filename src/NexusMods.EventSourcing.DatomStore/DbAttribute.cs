using System;

namespace NexusMods.EventSourcing.DatomStore;

public record DbAttribute(UInt128 UniqueId, ulong AttrEntityId, UInt128 ValueTypeId);
