using NexusMods.MneumonicDB.Abstractions.ElementComparers;
using NexusMods.MneumonicDB.Abstractions.Internals;
using NexusMods.MneumonicDB.Storage.Abstractions.ElementComparers;

namespace NexusMods.MneumonicDB.Abstractions.DatomComparators;

/// <summary>
/// The VAET Comparator.
/// </summary>
/// <typeparam name="TRegistry"></typeparam>
public class VAETComparator<TRegistry>(TRegistry registry) : ADatomComparator<
    UnmanagedValueComparer<EntityId, TRegistry>,
    AComparer<TRegistry>,
    EComparer<TRegistry>,
    TxComparer<TRegistry>,
    AssertComparer<TRegistry>,
    TRegistry>(registry)
    where TRegistry : IAttributeRegistry;
