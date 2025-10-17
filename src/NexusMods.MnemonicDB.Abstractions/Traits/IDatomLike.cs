using System.Collections.Generic;

namespace NexusMods.MnemonicDB.Abstractions.Traits;

public static class DatomLikeExtensions
{
    public static IEnumerable<Attribute<THigh,TLow,TSerializer>.ReadDatom> Resolved<THigh, TLow, TSerializer>(this IEnumerable<ValueDatom> src, Attribute<THigh, TLow, TSerializer> attr, AttributeResolver resolver) 
        where THigh : notnull 
        where TSerializer : IValueSerializer<TLow> 
        where TLow : notnull
    {
        foreach (var datom in src)
            yield return (Attribute<THigh,TLow,TSerializer>.ReadDatom)attr.Resolve(datom, resolver);
    }
    
    public static IEnumerable<IReadDatom> Resolved(this IEnumerable<ValueDatom> src, AttributeResolver resolver) 
    {
        foreach (var datom in src)
        {
            yield return resolver.Resolve(datom);
        }
    }
    
    public static IEnumerable<IReadDatom> Resolved(this IEnumerable<ValueDatom> src, IConnection connection) 
    {
        foreach (var datom in src)
        {
            yield return connection.AttributeResolver.Resolve(datom);
        }
    }
    
}
