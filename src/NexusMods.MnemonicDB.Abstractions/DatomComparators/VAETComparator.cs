using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.Internals;
using NexusMods.MnemonicDB.Storage.Abstractions.ElementComparers;

namespace NexusMods.MnemonicDB.Abstractions.DatomComparators;

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
