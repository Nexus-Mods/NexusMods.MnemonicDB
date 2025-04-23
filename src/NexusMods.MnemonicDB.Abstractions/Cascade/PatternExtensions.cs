using NexusMods.Cascade;
using NexusMods.Cascade.Rules;
using NexusMods.Cascade.Structures;

namespace NexusMods.MnemonicDB.Abstractions.Cascade;

public static partial class PatternExtensions
{
    [GenerateLVarOverrides]
    public static Pattern Db<TValue>(this Pattern pattern, LVar<EntityId> entity, IReadableAttribute<TValue> attribute, LVar<TValue> value) 
        where TValue : notnull
    {
        return pattern.With((Flow<KeyedValue<EntityId, TValue>>)attribute, entity, value);
    }

    [GenerateLVarOverrides]
    public static Pattern Db<TValue>(this Pattern pattern, LVar<EntityId> entity, IAttributeFlow<TValue> attribute, LVar<TValue> value, LVar<EntityId> txId) 
        where TValue : notnull
    {
        return pattern.With(attribute.AttributeWithTxIdFlow, entity, value, txId);
    }
    
    [GenerateLVarOverrides]
    public static Pattern DbOrDefault<TValue>(this Pattern pattern, LVar<EntityId> entity, IAttributeFlow<TValue> attribute, LVar<TValue> value) 
        where TValue : notnull
    {
        return pattern.WithDefault((Flow<KeyedValue<EntityId, TValue>>)attribute, entity, value);
    }
}
