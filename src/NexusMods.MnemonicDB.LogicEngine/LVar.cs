using System.Threading;

namespace NexusMods.MnemonicDB.LogicEngine;

public abstract record LVar(ulong Id, string? Name)
{
    internal static ulong NextId;
    public override string ToString() => Name != null ? $"?{Name}#{Id}" : $"?{Id}";
    
    public static LVar<T> Create<T>(string? name = null) => new(name);
}

public record LVar<T> : LVar
{ 
    internal LVar(ulong id, string? name) : base(id, name) {}
    internal LVar(string? name = null) : base(Interlocked.Increment(ref NextId), name){}
    
    public static LVar<T> Create(string? name = null) => new(name);
}

