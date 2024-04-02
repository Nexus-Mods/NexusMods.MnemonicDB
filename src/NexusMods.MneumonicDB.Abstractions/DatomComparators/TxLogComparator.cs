using NexusMods.MneumonicDB.Abstractions.ElementComparers;
using NexusMods.MneumonicDB.Abstractions.Internals;
using NexusMods.MneumonicDB.Storage.Abstractions.ElementComparers;

namespace NexusMods.MneumonicDB.Abstractions.DatomComparators;

/// <summary>
/// The TxLog Comparator.
/// </summary>
/// <typeparam name="TRegistry"></typeparam>
public class TxLogComparator<TRegistry> : ADatomComparator<
    TxComparer<TRegistry>,
    EComparer<TRegistry>,
    AComparer<TRegistry>,
    ValueComparer<TRegistry>,
    AssertComparer<TRegistry>,
    TRegistry>
    where TRegistry : IAttributeRegistry;
