using System;
using System.Collections.Generic;

namespace NexusMods.MnemonicDB.QueryEngine.Goals;

public class EmptyGoal(Env env) : IGoal
{
    public void Bind(Env _)
    {

    }

    public IEnumerable<Env> Execute(IEnumerable<Env> _)
    {
        yield return env;
    }
}
