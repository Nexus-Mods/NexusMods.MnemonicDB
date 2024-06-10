using NexusMods.MnemonicDB.Abstractions.ElementComparers;

namespace NexusMods.MnemonicDB.Abstractions.DatomComparators;

/// <summary>
/// VAE Comparator.
/// </summary>
public class VAEComparator : APartialDatomComparator<ValueComparer, AComparer, EComparer>;
