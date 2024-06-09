using NexusMods.MnemonicDB.Abstractions.ElementComparers;

namespace NexusMods.MnemonicDB.Abstractions.DatomComparators;

/// <summary>
/// EAV Comparator.
/// </summary>
public class EAVComparator : APartialDatomComparator<EComparer, AComparer, ValueComparer>;
