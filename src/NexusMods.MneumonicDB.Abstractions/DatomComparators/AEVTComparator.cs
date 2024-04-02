using NexusMods.MneumonicDB.Abstractions.ElementComparers;
using NexusMods.MneumonicDB.Abstractions.Internals;
using NexusMods.MneumonicDB.Storage.Abstractions.ElementComparers;

namespace NexusMods.MneumonicDB.Abstractions.DatomComparators;

/// <summary>
/// The AEVT Comparator.
/// </summary>
/// <typeparam name="TRegistry"></typeparam>
public class AEVTComparator<TRegistry> : ADatomComparator<
    AComparer<TRegistry>,
    EComparer<TRegistry>,
    ValueComparer<TRegistry>,
    TxComparer<TRegistry>,
    AssertComparer<TRegistry>,
    TRegistry>
    where TRegistry : IAttributeRegistry;
