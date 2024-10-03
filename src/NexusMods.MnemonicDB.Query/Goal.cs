
namespace NexusMods.MnemonicDB.Query;
using NexusMods.MnemonicDB.Abstractions;
using System;


public interface IFact
{
   Symbol Predicate { get; }
   int Arity { get; }
}

public static class Fact
{

    public static Fact<TArg1> Create<TArg1>(Symbol predicate, TArg1 Arg1)
    where TArg1 : IEquatable<TArg1> 
    {
        return new Fact<TArg1>(predicate, Arg1);
    }

    public static Fact<TArg1, TArg2> Create<TArg1, TArg2>(Symbol predicate, TArg1 Arg1, TArg2 Arg2)
    where TArg1 : IEquatable<TArg1> 
    where TArg2 : IEquatable<TArg2> 
    {
        return new Fact<TArg1, TArg2>(predicate, Arg1, Arg2);
    }

    public static Fact<TArg1, TArg2, TArg3> Create<TArg1, TArg2, TArg3>(Symbol predicate, TArg1 Arg1, TArg2 Arg2, TArg3 Arg3)
    where TArg1 : IEquatable<TArg1> 
    where TArg2 : IEquatable<TArg2> 
    where TArg3 : IEquatable<TArg3> 
    {
        return new Fact<TArg1, TArg2, TArg3>(predicate, Arg1, Arg2, Arg3);
    }

    public static Fact<TArg1, TArg2, TArg3, TArg4> Create<TArg1, TArg2, TArg3, TArg4>(Symbol predicate, TArg1 Arg1, TArg2 Arg2, TArg3 Arg3, TArg4 Arg4)
    where TArg1 : IEquatable<TArg1> 
    where TArg2 : IEquatable<TArg2> 
    where TArg3 : IEquatable<TArg3> 
    where TArg4 : IEquatable<TArg4> 
    {
        return new Fact<TArg1, TArg2, TArg3, TArg4>(predicate, Arg1, Arg2, Arg3, Arg4);
    }

    public static Fact<TArg1, TArg2, TArg3, TArg4, TArg5> Create<TArg1, TArg2, TArg3, TArg4, TArg5>(Symbol predicate, TArg1 Arg1, TArg2 Arg2, TArg3 Arg3, TArg4 Arg4, TArg5 Arg5)
    where TArg1 : IEquatable<TArg1> 
    where TArg2 : IEquatable<TArg2> 
    where TArg3 : IEquatable<TArg3> 
    where TArg4 : IEquatable<TArg4> 
    where TArg5 : IEquatable<TArg5> 
    {
        return new Fact<TArg1, TArg2, TArg3, TArg4, TArg5>(predicate, Arg1, Arg2, Arg3, Arg4, Arg5);
    }

    public static Fact<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6> Create<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6>(Symbol predicate, TArg1 Arg1, TArg2 Arg2, TArg3 Arg3, TArg4 Arg4, TArg5 Arg5, TArg6 Arg6)
    where TArg1 : IEquatable<TArg1> 
    where TArg2 : IEquatable<TArg2> 
    where TArg3 : IEquatable<TArg3> 
    where TArg4 : IEquatable<TArg4> 
    where TArg5 : IEquatable<TArg5> 
    where TArg6 : IEquatable<TArg6> 
    {
        return new Fact<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6>(predicate, Arg1, Arg2, Arg3, Arg4, Arg5, Arg6);
    }
}


public readonly record struct Fact<TArg1>(Symbol Predicate, TArg1 Arg1) : IFact
    where TArg1 : IEquatable<TArg1> 
{
    public int Arity => 1;

    public override string ToString()
    {
        return $"{Predicate}({string.Join(", ", new object[] { Arg1 })})";
    }
}


public readonly record struct Fact<TArg1, TArg2>(Symbol Predicate, TArg1 Arg1, TArg2 Arg2) : IFact
    where TArg1 : IEquatable<TArg1> 
    where TArg2 : IEquatable<TArg2> 
{
    public int Arity => 2;

    public override string ToString()
    {
        return $"{Predicate}({string.Join(", ", new object[] { Arg1, Arg2 })})";
    }
}


public readonly record struct Fact<TArg1, TArg2, TArg3>(Symbol Predicate, TArg1 Arg1, TArg2 Arg2, TArg3 Arg3) : IFact
    where TArg1 : IEquatable<TArg1> 
    where TArg2 : IEquatable<TArg2> 
    where TArg3 : IEquatable<TArg3> 
{
    public int Arity => 3;

    public override string ToString()
    {
        return $"{Predicate}({string.Join(", ", new object[] { Arg1, Arg2, Arg3 })})";
    }
}


public readonly record struct Fact<TArg1, TArg2, TArg3, TArg4>(Symbol Predicate, TArg1 Arg1, TArg2 Arg2, TArg3 Arg3, TArg4 Arg4) : IFact
    where TArg1 : IEquatable<TArg1> 
    where TArg2 : IEquatable<TArg2> 
    where TArg3 : IEquatable<TArg3> 
    where TArg4 : IEquatable<TArg4> 
{
    public int Arity => 4;

    public override string ToString()
    {
        return $"{Predicate}({string.Join(", ", new object[] { Arg1, Arg2, Arg3, Arg4 })})";
    }
}


public readonly record struct Fact<TArg1, TArg2, TArg3, TArg4, TArg5>(Symbol Predicate, TArg1 Arg1, TArg2 Arg2, TArg3 Arg3, TArg4 Arg4, TArg5 Arg5) : IFact
    where TArg1 : IEquatable<TArg1> 
    where TArg2 : IEquatable<TArg2> 
    where TArg3 : IEquatable<TArg3> 
    where TArg4 : IEquatable<TArg4> 
    where TArg5 : IEquatable<TArg5> 
{
    public int Arity => 5;

    public override string ToString()
    {
        return $"{Predicate}({string.Join(", ", new object[] { Arg1, Arg2, Arg3, Arg4, Arg5 })})";
    }
}


public readonly record struct Fact<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6>(Symbol Predicate, TArg1 Arg1, TArg2 Arg2, TArg3 Arg3, TArg4 Arg4, TArg5 Arg5, TArg6 Arg6) : IFact
    where TArg1 : IEquatable<TArg1> 
    where TArg2 : IEquatable<TArg2> 
    where TArg3 : IEquatable<TArg3> 
    where TArg4 : IEquatable<TArg4> 
    where TArg5 : IEquatable<TArg5> 
    where TArg6 : IEquatable<TArg6> 
{
    public int Arity => 6;

    public override string ToString()
    {
        return $"{Predicate}({string.Join(", ", new object[] { Arg1, Arg2, Arg3, Arg4, Arg5, Arg6 })})";
    }
}


public interface IGoal
{
   Symbol Predicate { get; }
   int Arity { get; }
}

public static class Goal
{

    public static Goal<TArg1> Create<TArg1>(Symbol predicate, Term<TArg1> Arg1)
    where TArg1 : IEquatable<TArg1> 
    {
        return new Goal<TArg1>(predicate, Arg1);
    }

    public static Goal<TArg1, TArg2> Create<TArg1, TArg2>(Symbol predicate, Term<TArg1> Arg1, Term<TArg2> Arg2)
    where TArg1 : IEquatable<TArg1> 
    where TArg2 : IEquatable<TArg2> 
    {
        return new Goal<TArg1, TArg2>(predicate, Arg1, Arg2);
    }

    public static Goal<TArg1, TArg2, TArg3> Create<TArg1, TArg2, TArg3>(Symbol predicate, Term<TArg1> Arg1, Term<TArg2> Arg2, Term<TArg3> Arg3)
    where TArg1 : IEquatable<TArg1> 
    where TArg2 : IEquatable<TArg2> 
    where TArg3 : IEquatable<TArg3> 
    {
        return new Goal<TArg1, TArg2, TArg3>(predicate, Arg1, Arg2, Arg3);
    }

    public static Goal<TArg1, TArg2, TArg3, TArg4> Create<TArg1, TArg2, TArg3, TArg4>(Symbol predicate, Term<TArg1> Arg1, Term<TArg2> Arg2, Term<TArg3> Arg3, Term<TArg4> Arg4)
    where TArg1 : IEquatable<TArg1> 
    where TArg2 : IEquatable<TArg2> 
    where TArg3 : IEquatable<TArg3> 
    where TArg4 : IEquatable<TArg4> 
    {
        return new Goal<TArg1, TArg2, TArg3, TArg4>(predicate, Arg1, Arg2, Arg3, Arg4);
    }

    public static Goal<TArg1, TArg2, TArg3, TArg4, TArg5> Create<TArg1, TArg2, TArg3, TArg4, TArg5>(Symbol predicate, Term<TArg1> Arg1, Term<TArg2> Arg2, Term<TArg3> Arg3, Term<TArg4> Arg4, Term<TArg5> Arg5)
    where TArg1 : IEquatable<TArg1> 
    where TArg2 : IEquatable<TArg2> 
    where TArg3 : IEquatable<TArg3> 
    where TArg4 : IEquatable<TArg4> 
    where TArg5 : IEquatable<TArg5> 
    {
        return new Goal<TArg1, TArg2, TArg3, TArg4, TArg5>(predicate, Arg1, Arg2, Arg3, Arg4, Arg5);
    }

    public static Goal<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6> Create<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6>(Symbol predicate, Term<TArg1> Arg1, Term<TArg2> Arg2, Term<TArg3> Arg3, Term<TArg4> Arg4, Term<TArg5> Arg5, Term<TArg6> Arg6)
    where TArg1 : IEquatable<TArg1> 
    where TArg2 : IEquatable<TArg2> 
    where TArg3 : IEquatable<TArg3> 
    where TArg4 : IEquatable<TArg4> 
    where TArg5 : IEquatable<TArg5> 
    where TArg6 : IEquatable<TArg6> 
    {
        return new Goal<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6>(predicate, Arg1, Arg2, Arg3, Arg4, Arg5, Arg6);
    }
}


public readonly record struct Goal<TArg1>(Symbol Predicate, Term<TArg1> Arg1) : IGoal
    where TArg1 : IEquatable<TArg1> 
{
    public int Arity => 1;

    public bool Matches(Fact<TArg1> fact)
    {
        if (!ReferenceEquals(Predicate, fact.Predicate))
        {
            return false;
        }

        return true;
    }

}


public readonly record struct Goal<TArg1, TArg2>(Symbol Predicate, Term<TArg1> Arg1, Term<TArg2> Arg2) : IGoal
    where TArg1 : IEquatable<TArg1> 
    where TArg2 : IEquatable<TArg2> 
{
    public int Arity => 2;

    public bool Matches(Fact<TArg1, TArg2> fact)
    {
        if (!ReferenceEquals(Predicate, fact.Predicate))
        {
            return false;
        }

        return true;
    }

}


public readonly record struct Goal<TArg1, TArg2, TArg3>(Symbol Predicate, Term<TArg1> Arg1, Term<TArg2> Arg2, Term<TArg3> Arg3) : IGoal
    where TArg1 : IEquatable<TArg1> 
    where TArg2 : IEquatable<TArg2> 
    where TArg3 : IEquatable<TArg3> 
{
    public int Arity => 3;

    public bool Matches(Fact<TArg1, TArg2, TArg3> fact)
    {
        if (!ReferenceEquals(Predicate, fact.Predicate))
        {
            return false;
        }

        return true;
    }

}


public readonly record struct Goal<TArg1, TArg2, TArg3, TArg4>(Symbol Predicate, Term<TArg1> Arg1, Term<TArg2> Arg2, Term<TArg3> Arg3, Term<TArg4> Arg4) : IGoal
    where TArg1 : IEquatable<TArg1> 
    where TArg2 : IEquatable<TArg2> 
    where TArg3 : IEquatable<TArg3> 
    where TArg4 : IEquatable<TArg4> 
{
    public int Arity => 4;

    public bool Matches(Fact<TArg1, TArg2, TArg3, TArg4> fact)
    {
        if (!ReferenceEquals(Predicate, fact.Predicate))
        {
            return false;
        }

        return true;
    }

}


public readonly record struct Goal<TArg1, TArg2, TArg3, TArg4, TArg5>(Symbol Predicate, Term<TArg1> Arg1, Term<TArg2> Arg2, Term<TArg3> Arg3, Term<TArg4> Arg4, Term<TArg5> Arg5) : IGoal
    where TArg1 : IEquatable<TArg1> 
    where TArg2 : IEquatable<TArg2> 
    where TArg3 : IEquatable<TArg3> 
    where TArg4 : IEquatable<TArg4> 
    where TArg5 : IEquatable<TArg5> 
{
    public int Arity => 5;

    public bool Matches(Fact<TArg1, TArg2, TArg3, TArg4, TArg5> fact)
    {
        if (!ReferenceEquals(Predicate, fact.Predicate))
        {
            return false;
        }

        return true;
    }

}


public readonly record struct Goal<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6>(Symbol Predicate, Term<TArg1> Arg1, Term<TArg2> Arg2, Term<TArg3> Arg3, Term<TArg4> Arg4, Term<TArg5> Arg5, Term<TArg6> Arg6) : IGoal
    where TArg1 : IEquatable<TArg1> 
    where TArg2 : IEquatable<TArg2> 
    where TArg3 : IEquatable<TArg3> 
    where TArg4 : IEquatable<TArg4> 
    where TArg5 : IEquatable<TArg5> 
    where TArg6 : IEquatable<TArg6> 
{
    public int Arity => 6;

    public bool Matches(Fact<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6> fact)
    {
        if (!ReferenceEquals(Predicate, fact.Predicate))
        {
            return false;
        }

        return true;
    }

}

