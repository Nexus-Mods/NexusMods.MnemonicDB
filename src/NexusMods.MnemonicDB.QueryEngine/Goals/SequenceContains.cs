using System.Collections.Generic;
using System.Linq;

namespace NexusMods.MnemonicDB.QueryEngine.Goals;

public class SequenceContains<T>(IEnumerable<T> seq, LVar<T> probe) : IGoal
{
    private ValueBox<T> _probe = null!;
    private readonly HashSet<T> _set = seq.ToHashSet();

    public void Bind(Env env)
    {
        _probe = env.Get(probe);
    }

    public IEnumerable<Env> Execute(IEnumerable<Env> env)
    {
        foreach (var e in env)
        {
            if (_set.Contains(_probe.Value!))
            {
                yield return e;
            }
        }
    }
}
