using System.Threading;

namespace NexusMods.MnemonicDB.LogicEngine;

/// <summary>
/// A logical variable, may eventually be bound in the environment to a value
/// </summary>
public struct LVar
{
    private static ulong _nextId = 0;
    internal LVar(ulong id, string? name = null)
    {
        Id = id;
        Name = name;
    }
    
    /// <summary>
    /// Create a new lvar with a unique id
    /// </summary>
    public static LVar Create(string name)
    {
        return new LVar(Interlocked.Increment(ref _nextId), name);
    }

    public string? Name { get; }
    public ulong Id { get; }

    public override string ToString() => 
        Name is not null ? $"LVar:{Name}({Id})" : $"LVar({Id})";
    
    public override bool Equals(object? obj) => obj is LVar other && other.Id == Id;

    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }
}
