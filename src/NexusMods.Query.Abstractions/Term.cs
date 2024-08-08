using System.Collections.Generic;
using DynamicData.Kernel;

namespace NexusMods.Query.Abstractions;

public record struct Term<T>(Optional<T> Constant, Optional<LVar<T>> Variable)
    where T : notnull
{
    public static Term<T> Value(T value) => new(value, Optional<LVar<T>>.None);
    public static Term<T> LVar(LVar<T> lvar) => new(Optional<T>.None, lvar);
    
    public static implicit operator Term<T>(T value) => Value(value);
    public static implicit operator Term<T>(LVar<T> value) => LVar(value);

    public void RegisterLVar(HashSet<ILVar> lVars)
    {
        if (Variable.HasValue)
        {
            lVars.Add(Variable.Value);
        }
    }
}
