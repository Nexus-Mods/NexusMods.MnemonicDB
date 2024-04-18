using System.Collections.Generic;

namespace NexusMods.MnemonicDB.QueryEngine.Goals;

public class DictionaryLookup<TK, TV>(IDictionary<TK, TV> src, LVar<TK> probe, LVar<TV> output) : IGoal
{
    private ValueBox<TK> _probe = null!;
    private ValueBox<TV> _output = null!;

    public void Bind(Env env)
    {
        _probe = env.Get(probe);
        _output = env.Get(output);
    }

    public IEnumerable<Env> Execute(IEnumerable<Env> env)
    {
        foreach (var e in env)
        {
            if (src.TryGetValue(_probe.Value!, out var value))
            {
                _output.Value = value;
                yield return e;
            }
        }
    }
}
