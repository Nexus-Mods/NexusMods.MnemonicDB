using NexusMods.MneumonicDB.Abstractions.Internals;
using NexusMods.MneumonicDB.Storage.Abstractions.ElementComparers;

namespace NexusMods.MneumonicDB.Storage.Abstractions.DatomComparators;

public class AEVTComparator<TRegistry> : ADatomComparator<
    AComparer<TRegistry>,
    EComparer<TRegistry>,
    ValueComparer<TRegistry>,
    TxComparer<TRegistry>,
    AssertComparer<TRegistry>,
    TRegistry>
    where TRegistry : IAttributeRegistry;
