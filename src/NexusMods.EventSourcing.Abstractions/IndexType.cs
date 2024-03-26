namespace NexusMods.EventSourcing.Abstractions;

public enum IndexType
{
    // Transaction log, the final source of truth, used
    // for replaying the database
    TxLog,
    // Primary index for looking up values on an entity
    EAVTHistory,
    EAVTCurrent,
    // Indexes for asking what entities have this attribute?
    AEVTHistory,
    AEVTCurrent,
    // Backref index for asking "who references this entity?"
    VAETCurrent,
    VAETHistory,
    // Secondary index for asking "who has this value on this attribute?"
    AVETCurrent,
    AVETHistory
}
