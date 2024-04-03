using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.Internals;
using NexusMods.MnemonicDB.Storage.Abstractions.ElementComparers;

namespace NexusMods.MnemonicDB.Abstractions.DatomComparators;

/// <summary>
/// The EAVT Comparator.
/// </summary>
/// <typeparam name="TRegistry"></typeparam>
public class EAVTComparator<TRegistry>(TRegistry registry) : ADatomComparator<
    EComparer<TRegistry>,
    AComparer<TRegistry>,
    ValueComparer<TRegistry>,
    TxComparer<TRegistry>,
    AssertComparer<TRegistry>,
    TRegistry>(registry)
    where TRegistry : IAttributeRegistry;

