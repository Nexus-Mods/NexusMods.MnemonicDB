using System.Collections.Generic;

namespace NexusMods.MnemonicDB.Abstractions.Traits;

public interface IDatomLikeRO : IKeyPrefixLikeRO
{
    /// <summary>
    /// Gets the value of the datom as an object.
    /// </summary>
    public object Value { get; }

    public IDatomLikeRO Retract()
    {
        var newPrefix = Prefix with { IsRetract = true, T = TxId.Tmp };
        return ValueDatom.Create(newPrefix, this);
    }
}

public interface IDatomLikeRO<out TValue> : IDatomLikeRO 
    where TValue : notnull
{
    /// <summary>
    /// Gets the value of the datom as a typed object.
    /// </summary>
    public TValue Value { get; }
}


public static class DatomLikeExtensions
{
    public static IEnumerable<Attribute<THigh,TLow,TSerializer>.ReadDatom> Resolved<THigh, TLow, TSerializer>(this IEnumerable<IDatomLikeRO> src, Attribute<THigh, TLow, TSerializer> attr, AttributeResolver resolver) 
        where THigh : notnull 
        where TSerializer : IValueSerializer<TLow> 
        where TLow : notnull
    {
        foreach (var datom in src)
            yield return (Attribute<THigh,TLow,TSerializer>.ReadDatom)attr.Resolve(datom, resolver);
    }
    
    public static IEnumerable<IReadDatom> Resolved(this IEnumerable<IDatomLikeRO> src, AttributeResolver resolver) 
    {
        foreach (var datom in src)
        {
            yield return resolver.Resolve(datom);
        }
    }
    
    public static IEnumerable<IReadDatom> Resolved(this IEnumerable<IDatomLikeRO> src, IConnection connection) 
    {
        foreach (var datom in src)
        {
            yield return connection.AttributeResolver.Resolve(datom);
        }
    }
    
}
