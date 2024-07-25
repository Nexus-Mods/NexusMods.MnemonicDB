using System;
using System.Collections.Generic;
using NexusMods.MnemonicDB.Queryable.AbstractSyntaxTree;

namespace NexusMods.MnemonicDB.Queryable.Engines.Lazy;

public record class StateMachine
{
    public Dictionary<ILVar, int> Variables { get; init; } = new();
    public ILVar[] Slots { get; init; } = [];

    public Func<ILVarBox[], bool>[] Steppers { get; init; } = [];
    
    
    public IEnumerable<TOut> Build<TIn, TOut>(TIn input, LVar<TIn> inputVar, LVar<TOut> output)
    {
        var inputIndex = Variables[inputVar];
        var outputIndex = Variables[output];
        
        var slots = new ILVarBox[Slots.Length];
        for (var i = 0; i < Slots.Length; i++)
        {
            slots[i] = Slots[i].MakeBox();
        }

        var idx = 0;
        
        var inputBox = (LVarBox<TIn>)slots[inputIndex];
        inputBox.Value = input;
        
        var outputBox = (LVarBox<TOut>)slots[outputIndex];
        
        while (true)
        {
            if (Steppers[idx](slots))
            {
                idx++;
                if (idx >= Steppers.Length)
                {
                    yield return outputBox.Value;
                    idx--;
                }
            }
            else
            {
                idx--;
                if (idx < 0)
                {
                    break;
                }
            }
        }
    }
}
