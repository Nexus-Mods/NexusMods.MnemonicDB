using System;
using System.Threading;

namespace NexusMods.MnemonicDB.Query;

public class LVar
{
    private static ulong _nextId;
    
    private readonly string _name;
    private readonly Type _type;
    private readonly ulong _id;

    private LVar(string name, Type type, ulong id)
    {
        _id = id;
        _name = name;
        _type = type;
    }
    
    /// <summary>
    /// The runtime type of the LVar
    /// </summary>
    public Type Type => _type;
    
    /// <summary>
    /// Create a new LVar of the given type
    /// </summary>
    public static LVar Create<T>(string name) => 
        new(name, typeof(T), Interlocked.Increment(ref _nextId));

    /// <summary>
    /// Reference equality semantics
    /// </summary>
    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj);
    }

    public override string ToString()
    {
        return $"LVar({_name})#{_id}";
    }
    
    public override int GetHashCode()
    {
        return _id.GetHashCode();
    }
}
