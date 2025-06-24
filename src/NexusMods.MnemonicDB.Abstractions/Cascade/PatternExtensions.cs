using NexusMods.Cascade;
using NexusMods.Cascade.Patterns;
using NexusMods.Cascade.Structures;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;

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

    /// <summary>
    /// Filters entities by those that have the given attribute
    /// </summary>
    public static Pattern HasAttribute<TValue>(this Pattern pattern, LVar<EntityId> entity, IAttributeFlow<TValue> attribute) where TValue : notnull
    {
        return pattern
            .MatchDefault(attribute.FlowFor(Query.Db)!, entity, out var value, default, default!)
            .IsNotDefault(value);
    }
    
    /// <summary>
    /// Filters entities by those that do not have the given attribute
    /// </summary>
    public static Pattern MissingAttribute<TValue>(this Pattern pattern, LVar<EntityId> entity, IAttributeFlow<TValue> attribute) where TValue : notnull
    {
        return pattern
            .MatchDefault(attribute.FlowFor(Query.Db)!, entity, out var value, default, default!)
            .IsDefault(value);
    }
    
    [GenerateLVarOverrides]
    public static Pattern DbOrDefault<TValue>(this Pattern pattern, LVar<EntityId> entity, IAttributeFlow<TValue> attribute, LVar<TValue> value, TValue? defaultValue = default)
        where TValue : notnull
    {
        return pattern.MatchDefault(attribute.FlowFor(Query.Db), entity, value, EntityId.MinValueNoPartition, defaultValue);
    }
    
    [GenerateLVarOverrides]
    public static Pattern DbOrDefault<TValue>(this Pattern pattern, Flow<IDb> dbFlow, LVar<EntityId> entity, IAttributeFlow<TValue> attribute, LVar<TValue> value, TValue? defaultValue = default)
        where TValue : notnull
    {
        return pattern.MatchDefault(attribute.FlowFor(dbFlow), entity, value, EntityId.MinValueNoPartition, defaultValue);
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

    /// <summary>
    /// A pattern that gets the latest TxId for the given entity, if the entity is updated, the pattern will be re-evaluated
    /// </summary>
    [GenerateLVarOverrides]
    public static Pattern DbLatestTx(this Pattern pattern, LVar<EntityId> entity, LVar<EntityId> txId)
    {
        return pattern.Match(Query.LatestTxForEntity, entity, txId);
    }
    
}
