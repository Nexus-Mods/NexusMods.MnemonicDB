using System;
using System.Collections.Generic;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.QueryEngine.Facts;

namespace NexusMods.MnemonicDB.QueryEngine.Ops;

public class Select<TPrevFact, TResultFact> : IOp 
    where TResultFact : IFact 
    where TPrevFact : IFact
{
    private readonly IOp _child;
    private readonly Func<TPrevFact,TResultFact> _selectFn;

    public Select(IOp child, LVar[] selectLVars)
    {
        _child = child;
        _selectFn = IFact.GetSelector<TPrevFact, TResultFact>(child.LVars, selectLVars);
        LVars = selectLVars;
    }
    public ITable Execute(IDb db)
    {
        var childTable = _child.Execute(db);
        var facts = new List<TResultFact>();
        foreach (var fact in ((ITable<TPrevFact>)_child.Execute(db)).Facts)
        {
            var newFact = _selectFn(fact);
            facts.Add(newFact);
        }
        return new ListTable<TResultFact>(LVars, facts);
        
    }

    public LVar[] LVars { get; }
    public Type FactType => typeof(TResultFact);
}
