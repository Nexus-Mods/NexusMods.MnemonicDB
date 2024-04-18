using System;
using System.Collections;
using System.Collections.Generic;

namespace NexusMods.MnemonicDB.QueryEngine;

public class Env
{
    private readonly IValueBox[] _slots;

    public Env(List<ILVar> lvars)
    {
        _slots = GC.AllocateUninitializedArray<IValueBox>(lvars.Count);
        for (var i = 0; i < lvars.Count; i++)
        {
            _slots[i] = lvars[i].MakeBox();
        }
    }

    public ValueBox<T> Get<T>(LVar<T> src)
    {
        return (ValueBox<T>)_slots[src.Id];
    }
}
