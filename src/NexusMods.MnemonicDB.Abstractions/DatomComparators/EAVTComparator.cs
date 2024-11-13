using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Storage.Abstractions.ElementComparers;

namespace NexusMods.MnemonicDB.Abstractions.DatomComparators;

/// <summary>
/// The EAVT Comparator.
/// </summary>
public sealed class EAVTComparator : ADatomComparator<
    EComparer,
    AComparer,
    ValueComparer,
    TxComparer>;

