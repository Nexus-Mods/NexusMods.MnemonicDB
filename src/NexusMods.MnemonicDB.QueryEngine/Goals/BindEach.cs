using System.Collections.Generic;

namespace NexusMods.MnemonicDB.QueryEngine.Goals;

public record BindEach<T>(IEnumerable<T> Source, LVar<T> Output) : IGoal
{
    private ValueBox<T> _output = null!;

    public void Bind(Env env)
    {
        _output = env.Get(Output);
    }

    public IEnumerable<Env> Execute(IEnumerable<Env> env)
    {
        foreach (var e in env)
        {
            foreach (var item in Source)
            {
                _output.Value = item;
                yield return e;
            }
        }
    }
}
