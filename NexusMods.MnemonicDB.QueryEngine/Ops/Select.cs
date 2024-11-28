using System;
using System.Collections.Generic;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.QueryEngine.AST;
using NexusMods.MnemonicDB.QueryEngine.Facts;

namespace NexusMods.MnemonicDB.QueryEngine.Ops;

public class Select<TPrevFact, TResultFact> : IOp 
    where TResultFact : IFact 
    where TPrevFact : IFact
{
    private readonly IOp _child;
    private readonly Func<TPrevFact,TResultFact> _selectFn;
    private readonly LVar[] _selectLVars;

    public Select(IOp child, SelectNode ast)
    {
        _child = child;
        _selectFn = IFact.GetSelector<TPrevFact, TResultFact>(ast.Children[0].EnvironmentExit, ast.EnvironmentExit);
        _selectLVars = ast.EnvironmentExit;
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
        return new ListTable<TResultFact>(_selectLVars, facts);
        
    }
}
