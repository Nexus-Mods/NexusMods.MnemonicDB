using System;
using System.Collections.Generic;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.QueryEngine.AST;
using NexusMods.MnemonicDB.QueryEngine.Facts;
using R3;

namespace NexusMods.MnemonicDB.QueryEngine.Ops;

public class Select<TPrevFact, TResultFact> : IOp<TResultFact>
    where TResultFact : IFact 
    where TPrevFact : IFact
{
    private readonly IOp<TPrevFact> _child;
    private readonly Func<TPrevFact,TResultFact> _selectFn;
    private readonly LVar[] _selectLVars;

    public Select(IOp child, SelectNode ast)
    {
        _child = (IOp<TPrevFact>)child;
        _selectFn = IFact.GetSelector<TPrevFact, TResultFact>(ast.Children[0].EnvironmentExit, ast.EnvironmentExit);
        _selectLVars = ast.EnvironmentExit;
    }
    public ITable<TResultFact> Execute(IDb db)
    {
        var facts = new List<TResultFact>();
        foreach (var fact in _child.Execute(db))
        {
            var newFact = _selectFn(fact);
            facts.Add(newFact);
        }
        return new ListTable<TResultFact>(_selectLVars, facts);
    }

    public IObservable<FactDelta<TResultFact>> Observe(IConnection conn)
    {
        throw new NotImplementedException();
    }
}
