using System;
using System.Collections;
using System.Collections.Generic;

namespace NexusMods.MnemonicDB.QueryEngine;

public class QueryDescription
{
    private readonly List<IGoal> _goals = new();
    private int nextVarId = 0;

    private List<ILVar> _vars = new();
    public List<ILVar> Vars => _vars;

    public List<IGoal> Goals => _goals;

    public LVar<TVal> New<TVal>()
    {
        var lvar = new LVar<TVal>(nextVarId++);
        _vars.Add(lvar);
        return lvar;
    }

    public void AddGoal(IGoal goal)
    {
        _goals.Add(goal);
    }

    public IEnumerable<Env> Execute()
    {
        var env = new Env(_vars);
        throw new NotImplementedException();

    }
}
