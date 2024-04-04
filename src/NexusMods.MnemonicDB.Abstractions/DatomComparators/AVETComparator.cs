using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.Internals;
using NexusMods.MnemonicDB.Storage.Abstractions.ElementComparers;

namespace NexusMods.MnemonicDB.Abstractions.DatomComparators;

/// <summary>
/// The AVET Comparator.
/// </summary>
/// <typeparam name="TRegistry"></typeparam>
public class AVETComparator<TRegistry>(TRegistry registry) : ADatomComparator<
    AComparer<TRegistry>,
    ValueComparer<TRegistry>,
    EComparer<TRegistry>,
    TxComparer<TRegistry>,
    AssertComparer<TRegistry>,
    TRegistry>(registry)
    where TRegistry : IAttributeRegistry;
