﻿using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.Internals;
using NexusMods.MnemonicDB.Storage.Abstractions.ElementComparers;

namespace NexusMods.MnemonicDB.Abstractions.DatomComparators;

/// <summary>
/// The AVET Comparator.
/// </summary>
public sealed class AVETComparator : ADatomComparator<
    AComparer,
    ValueComparer,
    EComparer,
    TxComparer>;
