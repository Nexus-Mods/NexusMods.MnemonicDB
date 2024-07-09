namespace MnemonicDB.Query;

public class Unification
{
    public static bool TryUnify<TA, TEnv>(TEnv env, Term<TA> a, Term<TA> b, out TEnv nextEnv)
        where TEnv : IEnvironment<TEnv>
    {
        if (a.Grounded && b.Grounded)
        {
            if (a.Constant!.Equals(b.Constant))
            {
                nextEnv = env;
                return true;
            }
            nextEnv = default!;
            return false;
        }

        if (a.Grounded)
        {
            if (env.TryGet(b, out var value1))
            {
                if (a.Constant!.Equals(value1))
                {
                    nextEnv = env;
                    return true;
                }
                nextEnv = default!;
                return false;
            }
            nextEnv = env.Bind(b, a.Constant!);
            return true;
        }

        if (b.Grounded)
        {
            if (env.TryGet(a, out var value2))
            {
                if (b.Constant!.Equals(value2))
                {
                    nextEnv = env;
                    return true;
                }
                nextEnv = default!;
                return false;
            }
            nextEnv = env.Bind(a, b.Constant!);
            return true;
        }

        if (env.TryGet(a, out var valueA1))
        {
            if (env.TryGet(b, out var valueB1))
            {
                if (valueA1.Equals(valueB1))
                {
                    nextEnv = env;
                    return true;
                }
                nextEnv = default!;
                return false;
            }
            nextEnv = env.Bind(b, valueA1);
            return true;
        }

        if (env.TryGet(b, out var value3))
        {
            nextEnv = env.Bind(a, value3);
            return true;
        }

        nextEnv = env.Bind(a, b.LVar);
        return true;
    }
    
    public bool TryUnify<TA, TEnv>(TEnv env, Fact<TA> a, Fact<TA> b, out TEnv nextEnv)
        where TEnv : IEnvironment<TEnv>
    {
        if (!ReferenceEquals(a.Predicate, b.Predicate))
        {
            nextEnv = default!;
            return false;
        }

        if (!TryUnify(env, a.A, b.A, out nextEnv))
        {
            return false;
        }

        return true;
    }
    
    public bool TryUnify<TA, TB, TEnv>(TEnv env, Fact<TA, TB> a, Fact<TA, TB> b, out TEnv nextEnv)
        where TEnv : IEnvironment<TEnv>
    {
        if (!ReferenceEquals(a.Predicate, b.Predicate))
        {
            nextEnv = default!;
            return false;
        }
        
        if (a.Arity != b.Arity)
        {
            nextEnv = default!;
            return false;
        }

        if (!TryUnify(env, a.A, b.A, out nextEnv))
        {
            return false;
        }

        if (!TryUnify(nextEnv, a.B, b.B, out nextEnv))
        {
            return false;
        }

        return true;
    }
}
