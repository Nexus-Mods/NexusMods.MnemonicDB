using System;
using NexusMods.MnemonicDB.Abstractions.DatomComparators;
using NexusMods.MnemonicDB.Abstractions.DatomIterators;
using NexusMods.MnemonicDB.Abstractions.Internals;
// ReSharper disable InconsistentNaming

namespace NexusMods.MnemonicDB.Abstractions;

/// <summary>
/// The type of indexes in the database
/// </summary>
public enum IndexType : byte
{
    /// <summary>
    /// Default for datoms that are not part of an index
    /// </summary>
    None = 0,
    
    /// <summary>
    /// Transaction log index
    /// </summary>
    TxLog = 1,

    /// <summary>
    /// Current row-level index
    /// </summary>
    EAVTCurrent = 2,
    
    /// <summary>
    /// History row-level index
    /// </summary>
    EAVTHistory,

    /// <summary>
    /// Current column-level index
    /// </summary>
    AEVTCurrent,
    
    /// <summary>
    /// History column-level index
    /// </summary>
    AEVTHistory,

    /// <summary>
    ///  Current reverse reference index
    /// </summary>
    VAETCurrent,
    
    /// <summary>
    /// History reverse reference index
    /// </summary>
    VAETHistory,

    /// <summary>
    /// Current indexed value index
    /// </summary>
    AVETCurrent,
    
    /// <summary>
    /// History indexed value index
    /// </summary>
    AVETHistory
}

/// <summary>
/// Extension methods for the IndexType enum
/// </summary>
public static class IndexTypeExtensions
{
    /// <summary>
    /// Get a comparator for the given index type
    /// </summary>
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
