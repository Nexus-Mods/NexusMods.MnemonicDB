using System.Collections.Generic;

namespace NexusMods.MnemonicDB.QueryEngine;

public interface IGoal
{
    public void Bind(Env env);

    public IEnumerable<Env> Execute(IEnumerable<Env> env);
}
