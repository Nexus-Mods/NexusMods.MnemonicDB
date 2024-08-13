using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using NexusMods.Query.Abstractions.Engines.Slots;

namespace NexusMods.Query.Abstractions.Engines.Abstract;


public struct ConstantSlot<T> : ISlot<T>
{
    private readonly T _value;

    public ConstantSlot(T value)
    {
        _value = value;
    }

    public T Get(ref Environment environment) => _value;
    public void Set(ref Environment environment, T value) => throw new InvalidOperationException("Cannot set a constant slot");
}

public abstract class APredicate<TA, TB, TC, TD> : IPredicate<TA, TB, TC, TD> 
    where TA : notnull 
    where TB : notnull 
    where TC : notnull 
    where TD : notnull
{
    public Environment.Execute Emit(BindingType[] bindingTypes, EnvironmentDefinition env, IArgument[] args, Environment.Execute innerExpr)
    {
        var bindingTuple = (bindingTypes[0], bindingTypes[1], bindingTypes[2], bindingTypes[3]);

        string methodName = bindingTuple switch
        {
            (BindingType.Constant, BindingType.Variable, BindingType.Constant, BindingType.Output) => nameof(EmitCVCO),
            (BindingType.Constant, BindingType.Output, BindingType.Constant, BindingType.Constant) => nameof(EmitCOCC),
            (BindingType.Constant, BindingType.Variable, BindingType.Constant, BindingType.Variable) => nameof(EmitCCCC),
            (BindingType.Constant, BindingType.Variable, BindingType.Constant, BindingType.Constant) => nameof(EmitCCCC),
            (BindingType.Constant, BindingType.Output, BindingType.Constant, BindingType.Variable) => nameof(EmitCOCC),
            _ => throw new InvalidOperationException($"Unknown binding type combination {bindingTuple}")
        };
        
        var method = GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
        if (method == null)
            throw new InvalidOperationException("Unknown binding type");
        
        var aSlot = ResolveArgument<TA>(env, args[0]);
        var bSlot = ResolveArgument<TB>(env, args[1]);
        var cSlot = ResolveArgument<TC>(env, args[2]);
        var dSlot = ResolveArgument<TD>(env, args[3]);
        
        var genericMethod = method.MakeGenericMethod(aSlot.GetType(), bSlot.GetType(), cSlot.GetType(), dSlot.GetType());
        return (Environment.Execute)genericMethod.Invoke(this, [aSlot, bSlot, cSlot, dSlot, innerExpr])!;
        

        //return Emit(aExpr, bExpr, cExpr, dExpr, innerExpr);
        return null!;
    }
    protected virtual Environment.Execute EmitCOCC<TSlotA, TSlotB, TSlotC, TSlotD>(TSlotA a, TSlotB b, TSlotC c, TSlotD d, Environment.Execute innerExpression)
        where TSlotA : ISlot<TA>
        where TSlotB : ISlot<TB>
        where TSlotC : ISlot<TC>
        where TSlotD : ISlot<TD>
    {
        throw new NotSupportedException($"This predicate {this} does not support the given binding type: COCC");
    }
    
    protected virtual Environment.Execute EmitCCCC<TSlotA, TSlotB, TSlotC, TSlotD>(TSlotA a, TSlotB b, TSlotC c, TSlotD d, Environment.Execute innerExpression)
        where TSlotA : ISlot<TA>
        where TSlotB : ISlot<TB>
        where TSlotC : ISlot<TC>
        where TSlotD : ISlot<TD>
    {
        throw new NotSupportedException($"This predicate {this} does not support the given binding type: CCCC");
    }
    
    protected virtual Environment.Execute EmitCVCO<TSlotA, TSlotB, TSlotC, TSlotD>(TSlotA a, TSlotB b, TSlotC c, TSlotD d, Environment.Execute innerExpression)
        where TSlotA : ISlot<TA>
        where TSlotB : ISlot<TB>
        where TSlotC : ISlot<TC>
        where TSlotD : ISlot<TD>
    {
        throw new NotSupportedException($"This predicate {this} does not support the given binding type: CVCO");
    }

    //protected abstract Expression Emit(BindingType[] bindingTypes, Expression a, Expression b, Expression c, Expression d, Expression innerExpression);

    private static ISlot<T> ResolveArgument<T>(EnvironmentDefinition env, IArgument arg) where T : notnull
    {
        ISlot<T> slot;
        if (arg.TryGetVariable(out var foundA))
            slot = env.GetSlot((Variable<T>)foundA);
        else if (arg.TryGetConstant<T>(out var constantA))
            slot = new ConstantSlot<T>(constantA);
        else 
            throw new InvalidOperationException("Unknown argument type");
        return slot;
    }
}
