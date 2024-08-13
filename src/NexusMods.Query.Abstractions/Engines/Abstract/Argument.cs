using System;
using DynamicData.Kernel;

namespace NexusMods.Query.Abstractions.Engines.Abstract;

public record Argument<T>(Optional<T> Constant, Optional<Variable<T>> Variable) : IArgument
    where T : notnull
{
    public Type Type => typeof(T);
    public bool TryGetVariable(out IVariable variable)
    {
        if (Variable.HasValue)
        {
            variable = Variable.Value!;
            return true;
        }
        else
        {
            variable = default!;
            return false;
        }
    }

    public bool TryGetConstant<T1>(out T1 constant)
    {
        if (Constant.HasValue)
        {
            constant = (T1)(object)Constant.Value!;
            return true;
        }
        else
        {
            constant = default!;
            return false;
        }
    }

    public static Argument<T> New(T value)
        => new(value, Optional<Variable<T>>.None);

    public static Argument<T> New(Variable<T> value)
        => new(Optional<T>.None, value);
    
    public static implicit operator Argument<T> (T value)
        => New(value);
    
    public static implicit operator Argument<T> (Variable<T> value)
        => New(value);

    public override string ToString()
    {
        if (Constant.HasValue)
        {
            return Constant.Value!.ToString()!;
        }
        else if (Variable.HasValue)
        {
            return Variable.Value.ToString();
        }
        else
        {
            throw new InvalidOperationException("Argument is neither a constant nor a variable");
        }
    }
}
