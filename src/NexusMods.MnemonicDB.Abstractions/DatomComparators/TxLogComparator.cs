using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.Internals;
using NexusMods.MnemonicDB.Storage.Abstractions.ElementComparers;

namespace NexusMods.MnemonicDB.Abstractions.DatomComparators;

/// <summary>
/// The TxLog Comparator.
/// </summary>
/// <typeparam name="TRegistry"></typeparam>
public class TxLogComparator<TRegistry>(TRegistry registry) : ADatomComparator<
    TxComparer<TRegistry>,
    EComparer<TRegistry>,
    AComparer<TRegistry>,
    ValueComparer<TRegistry>,
    AssertComparer<TRegistry>,
    TRegistry>(registry)
    where TRegistry : IAttributeRegistry;
