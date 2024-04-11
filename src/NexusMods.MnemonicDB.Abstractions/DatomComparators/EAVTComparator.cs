using NexusMods.MnemonicDB.Abstractions.DatomIterators;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.Internals;
using NexusMods.MnemonicDB.Storage.Abstractions.ElementComparers;

namespace NexusMods.MnemonicDB.Abstractions.DatomComparators;

/// <summary>
/// The EAVT Comparator.
/// </summary>
/// <typeparam name="TRegistry"></typeparam>
public sealed class EAVTComparator : ADatomComparator<
    EComparer,
    AComparer,
    ValueComparer,
    TxComparer,
    AssertComparer>;

