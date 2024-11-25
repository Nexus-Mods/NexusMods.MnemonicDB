using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.MnemonicDB.QueryEngine;

public abstract class Rule
{
    public abstract int Arity { get; }
    
    public abstract Symbol Name { get; }
}

public abstract class Rule<T1, T2>
{
}

public abstract class Rule<T1, T2, T3>
{
}
