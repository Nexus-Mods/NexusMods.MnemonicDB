using NexusMods.MnemonicDB.Abstractions.ElementComparers;

namespace NexusMods.MnemonicDB.Abstractions;

/// <summary>
///     A record of information that maps a in-code version of an attribute (the symbol name) to the
///     database attribute entity Id, and the ValueType id
/// </summary>
/// <param name="UniqueId"></param>
/// <param name="AttrEntityId"></param>
/// <param name="LowLevelType"></param>
public record DbAttribute(Symbol UniqueId, AttributeId AttrEntityId, ValueTags LowLevelType, IAttribute Attribute);
