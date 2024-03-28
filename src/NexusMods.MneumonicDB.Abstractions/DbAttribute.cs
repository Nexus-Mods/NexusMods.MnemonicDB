namespace NexusMods.MneumonicDB.Abstractions;

/// <summary>
///     A record of information that maps a in-code version of an attribute (the symbol name) to the
///     database attribute entity Id, and the ValueType id
/// </summary>
/// <param name="UniqueId"></param>
/// <param name="AttrEntityId"></param>
/// <param name="ValueTypeId"></param>
public record DbAttribute(Symbol UniqueId, AttributeId AttrEntityId, Symbol ValueTypeId);
