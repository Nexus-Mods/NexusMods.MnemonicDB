using System;
using NexusMods.MnemonicDB.Abstractions.DatomComparators;
using NexusMods.MnemonicDB.Abstractions.DatomIterators;
using NexusMods.MnemonicDB.Abstractions.Internals;

namespace NexusMods.MnemonicDB.Abstractions;

public enum IndexType : byte
{
    // Transaction log, the final source of truth, used
    // for replaying the database
    TxLog = 1,

    // Primary index for looking up values on an entity
    EAVTCurrent = 2,
    EAVTHistory,

    // Indexes for asking what entities have this attribute?
    AEVTCurrent,
    AEVTHistory,

    // Backref index for asking "who references this entity?"
    VAETCurrent,
    VAETHistory,

    // Secondary index for asking "who has this value on this attribute?"
    AVETCurrent,
    AVETHistory
}


public static class IndexTypeExtensions
{
    public static IDatomComparator GetComparator(this IndexType type)
    {
        return type switch
        {
            IndexType.EAVTCurrent => new EAVTComparator(),
            IndexType.EAVTHistory => new EAVTComparator(),
            IndexType.AEVTCurrent => new AEVTComparator(),
            IndexType.AEVTHistory => new AEVTComparator(),
            IndexType.VAETCurrent => new VAETComparator(),
            IndexType.VAETHistory => new VAETComparator(),
            IndexType.AVETCurrent => new AVETComparator(),
            IndexType.AVETHistory => new AVETComparator(),
            IndexType.TxLog => new TxLogComparator(),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown index type")
        };
    }

    /// <summary>
    /// For a member of the given Current/History pair, return the current variant.
    /// </summary>
    public static IndexType CurrentVariant(this IndexType type)
    {
        if (type <= IndexType.TxLog)
            return type;

        return (int)type % 2 == 0 ? type : type - 1;
    }

    /// <summary>
    /// For a member of the given Current/History pair, return the history variant.
    /// </summary>
    public static IndexType HistoryVariant(this IndexType type)
    {
        if (type <= IndexType.TxLog)
            return type;

        return (int)type % 2 == 0 ? type + 1 : type;
    }


}
