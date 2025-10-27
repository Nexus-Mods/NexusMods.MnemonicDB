using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.Internals;

namespace NexusMods.MnemonicDB.Abstractions.Traits;

/// <summary>
/// An interface for an object that only has a read-only view of a KeyPrefix
/// </summary>
public interface IKeyPrefixLike
{
    public KeyPrefix Prefix { get; }
    
    public EntityId E => Prefix.E;
    
    public AttributeId A => Prefix.A;

    public TxId T => Prefix.T;
    
    public ValueTag ValueTag => Prefix.ValueTag;
    
    public bool IsRetract => Prefix.IsRetract;

    public IndexType IndexType => Prefix.Index;
}
