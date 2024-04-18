using System.Collections.Generic;

namespace NexusMods.MnemonicDB.QueryEngine.Goals;

public record Unify<T>(LVar<T> A, LVar<T> B) : IGoal
{
    private ValueBox<T> _a = null!;
    private ValueBox<T> _b = null!;

    public void Bind(Env env)
    {
        _a = env.Get(A);
        _b = env.Get(B);
    }

    public IEnumerable<Env> Execute(IEnumerable<Env> env)
    {
        foreach (var e in env)
        {
            if (_a.Value!.Equals(_b.Value))
            {
                yield return e;
            }
        }
    }
}
