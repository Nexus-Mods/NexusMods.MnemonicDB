using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.Internals;

namespace NexusMods.MnemonicDB.Abstractions.Traits;


/// <summary>
/// An interface for an object that only has a read-only view of a KeyPrefix
/// </summary>
public interface IKeyPrefixLikeRO
{
    public KeyPrefix Prefix { get; }
    
    public EntityId E => Prefix.E;
    
    public AttributeId A => Prefix.A;

    public TxId T => Prefix.T;
    
    public ValueTag ValueTag => Prefix.ValueTag;
    
    public bool IsRetract => Prefix.IsRetract;

    public IndexType IndexType => Prefix.Index;
}

/// <summary>
/// An interface for an object that has a read-write view of a KeyPrefix
/// </summary>
public interface IKeyPrefixRW
{
    public ref KeyPrefix Prefix { get; }

    public EntityId E
    {
        get => Prefix.E;
        set => Prefix.E = value;
    }
    
    public AttributeId A
    {
        get => Prefix.A;
        set => Prefix.A = value;
    }
    
    public TxId T
    {
        get => Prefix.T;
        set => Prefix.T = value;
    }
    
    public ValueTag ValueTag
    {
        get => Prefix.ValueTag;
        set => Prefix.ValueTag = value;
    }
    
    public IndexType IndexType
    {
        get => Prefix.Index;
        set => Prefix.Index = value;
    }
    
    public bool IsRetract
    {
        get => Prefix.IsRetract;
        set => Prefix.IsRetract = value;
    }
}
