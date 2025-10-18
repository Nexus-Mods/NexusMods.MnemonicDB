using System.Collections.Generic;

namespace NexusMods.MnemonicDB.Abstractions;

public class ResolvedDatoms : List<ResolvedDatom>
{
    public ResolvedDatoms(Datoms datoms, AttributeResolver resolver) : base(datoms.Count)
    {
        foreach (var datom in datoms)
        {
            Add(new ResolvedDatom(datom, resolver));
        }
    }

    public IEnumerable<THighLevel> GetAll<THighLevel, TLowLevel, TSerializer>(Attribute<THighLevel, TLowLevel, TSerializer> attribute) 
        where THighLevel : notnull 
        where TLowLevel : notnull
        where TSerializer : IValueSerializer<TLowLevel>
    {
        foreach (var datom in this)
        {
            if (ReferenceEquals(datom.A, attribute))
                yield return (THighLevel)datom.V;
        }
    }

    public THighLevel GetFirst<THighLevel, TLowLevel, TSerializer>(Attribute<THighLevel, TLowLevel, TSerializer> attribute) 
        where THighLevel : notnull 
        where TLowLevel : notnull 
        where TSerializer : IValueSerializer<TLowLevel>
    {
        foreach (var datom in this)
        {
            if (ReferenceEquals(datom.A, attribute))
                return (THighLevel)datom.V;
        }
        throw new KeyNotFoundException($"Attribute {attribute} not found in datoms");
    }

    public bool Contains(IAttribute attr)
    {
        foreach (var datom in this)
            if (ReferenceEquals(datom.A, attr))
                return true;
        return false;
    }
}
