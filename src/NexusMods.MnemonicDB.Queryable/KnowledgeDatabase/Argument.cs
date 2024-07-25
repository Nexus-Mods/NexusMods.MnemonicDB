using DynamicData.Kernel;
using NexusMods.MnemonicDB.Queryable.AbstractSyntaxTree;

namespace NexusMods.MnemonicDB.Queryable;

public interface IArgument
{
    public bool IsVariable { get; }
    
    public ILVar Variable { get; }
}

public struct Argument<T>() : IArgument where T : notnull
{
    public Optional<T> Value { get; init; }

    public LVar<T> LVar { get; init; } = default!;
    
    
    
    /// <summary>
    /// True if this is a constant value, like `5` or `"hello"`.
    /// </summary>
    public bool IsConstant => !IsVariable;
    
    /// <summary>
    /// True if this is a variable, like `X` or `Y`.
    /// </summary>
    public bool IsVariable => !Value.HasValue;

    public ILVar Variable => LVar;

    public static implicit operator Argument<T>(T value) => new() { Value = value };

    public static implicit operator Argument<T>(LVar<T> lvar) => new() { LVar = lvar };

    public override string ToString()
    {
        if (IsVariable)
            return LVar.ToString()!;

        return Value.Value.ToString()!;
    }
}
