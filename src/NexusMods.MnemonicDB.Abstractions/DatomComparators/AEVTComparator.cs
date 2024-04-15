﻿using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.Internals;
using NexusMods.MnemonicDB.Storage.Abstractions.ElementComparers;

namespace NexusMods.MnemonicDB.Abstractions.DatomComparators;

/// <summary>
/// The AEVT Comparator.
/// </summary>
/// <typeparam name="TRegistry"></typeparam>
public class AEVTComparator : ADatomComparator<
    AComparer,
    EComparer,
    ValueComparer,
    TxComparer,
    AssertComparer>;
