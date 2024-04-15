using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.Internals;
using NexusMods.MnemonicDB.Storage.Abstractions.ElementComparers;

namespace NexusMods.MnemonicDB.Abstractions.DatomComparators;

/// <summary>
/// The TxLog Comparator.
/// </summary>
public class TxLogComparator : ADatomComparator<
    TxComparer,
    EComparer,
    AComparer,
    ValueComparer,
    AssertComparer>;
