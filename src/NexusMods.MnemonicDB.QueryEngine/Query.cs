using System.Collections.Generic;
using NexusMods.MnemonicDB.QueryEngine.Goals;

namespace NexusMods.MnemonicDB.QueryEngine;

public static class Query
{
    public static QueryDescription New()
    {
        return new QueryDescription();
    }

    public static QueryDescription From<T>(this QueryDescription desc, IEnumerable<T> src, out LVar<T> outVar)
    {
        outVar = desc.New<T>();
        desc.AddGoal(new BindEach<T>(src, outVar));
        return desc;
    }

    public static QueryDescription From<TK, TV>(this QueryDescription desc, IDictionary<TK, TV> src, LVar<TK> probe, out LVar<TV> outVar)
    {
        outVar = desc.New<TV>();
        desc.AddGoal(new DictionaryLookup<TK, TV>(src, probe, outVar));
        return desc;
    }

    public static QueryDescription From<T>(this QueryDescription desc, IEnumerable<T> src, LVar<T> outVar)
    {
        desc.AddGoal(new SequenceContains<T>(src, outVar));
        return desc;
    }

    public static QueryDescription Eq<T>(this QueryDescription desc, LVar<T> a, LVar<T> b)
    {
        desc.AddGoal(new Unify<T>(a, b));
        return desc;
    }

    public static IEnumerable<TA> Run<TA>(this QueryDescription desc, LVar<TA> a)
    {
        var env = new Env(desc.Vars);

        var acc = new EmptyGoal(env).Execute([env]);

        foreach (var goal in desc.Goals)
        {
            goal.Bind(env);
            acc = goal.Execute(acc);
        }

        var finalSlots = env.Get(a);

        foreach (var e in acc)
        {
            yield return finalSlots.Value;
        }

    }

}
