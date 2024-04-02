using NexusMods.MneumonicDB.Abstractions;
using NexusMods.MneumonicDB.Abstractions.Internals;
using NexusMods.MneumonicDB.Storage.Abstractions.ElementComparers;

namespace NexusMods.MneumonicDB.Storage.Abstractions.DatomComparators;

public class VAETComparator<TRegistry> : ADatomComparator<
    UnmanagedValueComparer<EntityId, TRegistry>,
    AComparer<TRegistry>,
    EComparer<TRegistry>,
    TxComparer<TRegistry>,
    AssertComparer<TRegistry>,
    TRegistry>
    where TRegistry : IAttributeRegistry;
