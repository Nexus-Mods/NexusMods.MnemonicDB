using System;
using NexusMods.MneumonicDB.Abstractions.DatomComparators;
using NexusMods.MneumonicDB.Abstractions.DatomIterators;
using NexusMods.MneumonicDB.Abstractions.Internals;

namespace NexusMods.MneumonicDB.Abstractions;

public enum IndexType
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
    public static IDatomComparator<TRegistry> GetComparator<TRegistry>(this IndexType type, TRegistry registry)
        where TRegistry : IAttributeRegistry
    {
        return type switch
        {
            IndexType.EAVTCurrent => new EAVTComparator<TRegistry>(registry),
            IndexType.EAVTHistory => new EAVTComparator<TRegistry>(registry),
            IndexType.AEVTCurrent => new AEVTComparator<TRegistry>(registry),
            IndexType.AEVTHistory => new AEVTComparator<TRegistry>(registry),
            IndexType.VAETCurrent => new VAETComparator<TRegistry>(registry),
            IndexType.VAETHistory => new VAETComparator<TRegistry>(registry),
            IndexType.AVETCurrent => new AVETComparator<TRegistry>(registry),
            IndexType.AVETHistory => new AVETComparator<TRegistry>(registry),
            IndexType.TxLog => new TxLogComparator<TRegistry>(registry),
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
