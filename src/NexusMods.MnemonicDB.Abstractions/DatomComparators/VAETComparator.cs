using System;
using System.Runtime.CompilerServices;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.Internals;
using NexusMods.MnemonicDB.Storage.Abstractions.ElementComparers;

namespace NexusMods.MnemonicDB.Abstractions.DatomComparators;

/// <summary>
/// The VAET Comparator.
/// </summary>
public sealed class VAETComparator : ADatomComparator<
    ValueComparer,
    AComparer,
    EComparer,
    TxComparer>
{
}

