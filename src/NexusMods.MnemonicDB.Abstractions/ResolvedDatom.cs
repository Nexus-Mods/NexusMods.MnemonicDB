using System;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.Internals;

namespace NexusMods.MnemonicDB.Abstractions;

/// <summary>
///     Marker interface for a read datom that will contain a TX value.
/// </summary>
public readonly struct ResolvedDatom
{
    /// <summary>
    /// Creates a new instance of <see cref="ResolvedDatom"/> from a <see cref="Datom"/> and an <see cref="AttributeResolver"/>.
    /// </summary>
    public ResolvedDatom(Datom d, AttributeResolver resolver)
    {
        Prefix = d.Prefix;
        if (!resolver.TryGetAttribute(d.A, out var attr))
        {
            A = null!;
            V = d.V;
        }
        else
        {
            A = attr;
            V = attr.FromLowLevelObject(d.V, resolver);
        }
    }
    
    /// <summary>
    /// The KeyPrefix of this datom
    /// </summary>
    public KeyPrefix Prefix { get; }
    
    /// <summary>
    /// The Attribute instance of this datom.
    /// </summary>
    public IAttribute A { get; }
    
    /// <summary>
    /// The application level value of this datom. (not the DB level value)
    /// </summary>
    public object V { get; }
    
    /// <summary>
    /// Entity id of the datom.
    /// </summary>
    public EntityId E => Prefix.E;

    /// <summary>
    ///     The value type of the datom, this is used to find the correct serializer.
    /// </summary>
    public ValueTag Tag => Prefix.ValueTag;
    
    /// <summary>
    ///     The transaction id of the datom.
    /// </summary>
    public TxId T => Prefix.T;
    
    /// <summary>
    ///     True if this is a retraction of a previous datom.
    /// </summary>
    public bool IsRetract => Prefix.IsRetract;
    
    /// <summary>
    /// Returns true if the datom is equal when comparing the entity, attribute and value.
    /// </summary>
    public bool EqualsByValue(ResolvedDatom other) => A.Equals(other.A) && E.Equals(other.E) && V.Equals(other.V);
    
    /// <summary>
    /// Hashcode of just the entity, attribute and value.
    /// </summary>
    public int HashCodeByValue() => HashCode.Combine(E, A, V);
    
    /// <summary>
    /// (E, A, V) deconstructor
    /// </summary>
    public void Deconstruct(out EntityId e, out AttributeId a, out object v)
    {
        e = Prefix.E;
        a = Prefix.A;
        v = V;   
    }
    
    /// <summary>
    /// (E, A, V, T) deconstuctor   
    /// </summary>
    /// <param name="e"></param>
    /// <param name="a"></param>
    /// <param name="v"></param>
    /// <param name="t"></param>
    public void Deconstruct(out EntityId e, out AttributeId a, out object v, out TxId t)
    {
        e = Prefix.E;
        a = Prefix.A;
        v = V;
        t = Prefix.T;
    }

    /// <summary>
    /// (E, A, V, T, IsRetract) deconstuctor  
    /// </summary>
    public void Deconstruct(out EntityId e, out AttributeId a, out object v, out TxId t, out bool isRetract)
    {
        e = Prefix.E;
        a = Prefix.A;
        v = V;
        t = Prefix.T;
        isRetract = Prefix.IsRetract;
    }
}

