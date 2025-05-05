using NexusMods.Cascade;
using NexusMods.Cascade.Patterns;
using NexusMods.Cascade.Structures;

namespace NexusMods.MnemonicDB.Abstractions.Cascade;

public static partial class PatternExtensions
{
    [GenerateLVarOverrides]
    public static Pattern Db<TValue>(this Pattern pattern, LVar<EntityId> entity, IReadableAttribute<TValue> attribute, LVar<TValue> value) 
        where TValue : notnull
    {
        return pattern.Match((Flow<KeyedValue<EntityId, TValue>>)attribute, entity, value);
    }
    
    [GenerateLVarOverrides]
    public static Pattern Db<TValue>(this Pattern pattern, Flow<IDb> dbFlow, LVar<EntityId> entity, IAttributeFlow<TValue> attribute, LVar<TValue> value) 
        where TValue : notnull
    {
        
        return pattern.Match(attribute.FlowFor(dbFlow), entity, value);
    }

    [GenerateLVarOverrides]
    public static Pattern Db<TValue>(this Pattern pattern, LVar<EntityId> entity, IAttributeFlow<TValue> attribute, LVar<TValue> value, LVar<EntityId> txId) 
        where TValue : notnull
    {
        return pattern.Match(attribute.AttributeWithTxIdFlow, entity, value, txId);
    }
    
    [GenerateLVarOverrides]
    public static Pattern DbOrDefault<TValue>(this Pattern pattern, Flow<IDb> dbFlow, LVar<EntityId> entity, IAttributeFlow<TValue> attribute, LVar<TValue> value) 
        where TValue : notnull
    {
        return pattern.MatchDefault(attribute.FlowFor(dbFlow), entity, value);
    }

    /// <summary>
    /// Matches against the full history of the attribute, for each time a attribute has changed, a new match will be created, and the txId will be populated
    /// with the transactionId of the matching transactio
    /// </summary>
    [GenerateLVarOverrides]
    public static Pattern DbHistory<TValue>(this Pattern pattern, LVar<EntityId> entity, IAttributeFlow<TValue> attribute, LVar<TValue> value, LVar<EntityId> txId) 
        where TValue : notnull
    {
        return pattern.Match(attribute.AttributeHistoryFlow, entity, value, txId);
    }
    
    /// <summary>
    /// Matches against the full history of the attribute, for each time a attribute has changed, a new match will be created, and the txId will be populated
    /// with the transactionId of the matching transactio
    /// </summary>
    [GenerateLVarOverrides]
    public static Pattern DbHistory<TValue, TOther>(this Pattern pattern, LVar<EntityId> entity, IAttributeFlow<TValue> attribute, LVar<TValue> value, LVar<EntityId> txId, IReadableAttribute<TOther> other, LVar<TOther> otherValue) 
        where TValue : notnull
        where TOther : notnull
    {
        return pattern.Match(attribute.AttributeHistoryFlowWithOther(other), entity, value, txId, otherValue);
    }
    
}
