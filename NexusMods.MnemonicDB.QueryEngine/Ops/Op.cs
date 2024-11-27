using System;
using System.Linq;

namespace NexusMods.MnemonicDB.QueryEngine.Ops;

public static class Op
{
    public static IOp Select(IOp prevOp, LVar[] selectVars)
    {
        var genericDefinition = Facts.IFact.TupleTypes[selectVars.Length]!;
        var specificType = genericDefinition.MakeGenericType(selectVars.Select(v => v.Type).ToArray());
        var selectOp = (IOp)Activator.CreateInstance(typeof(Select<,>).MakeGenericType(prevOp.FactType, specificType), prevOp, selectVars)!;
        return selectOp;
    }
    
}
