
using System;
namespace NexusMods.MnemonicDB.QueryEngine.Facts;

public readonly record struct Fact<T0>(T0 Item0) : IFact
    where T0 : notnull

{
    public int Arity => 1;

    public int GetHashCode(int i)
    {
        return i switch
        {

            0 => Item0.GetHashCode(),
            _ => throw new ArgumentOutOfRangeException(nameof(i)),
        };
    }
    
}

public readonly record struct Fact<T0, T1>(T0 Item0, T1 Item1) : IFact
    where T0 : notnull
    where T1 : notnull

{
    public int Arity => 2;

    public int GetHashCode(int i)
    {
        var v = (1, 2);
        return i switch
        {

            0 => Item0.GetHashCode(),
            1 => Item1.GetHashCode(),
            _ => throw new ArgumentOutOfRangeException(nameof(i)),
        };
    }
    
}

public readonly record struct Fact<T0, T1, T2>(T0 Item0, T1 Item1, T2 Item2) : IFact
    where T0 : notnull
    where T1 : notnull
    where T2 : notnull

{
    public int Arity => 3;

    public int GetHashCode(int i)
    {
        return i switch
        {

            0 => Item0.GetHashCode(),
            1 => Item1.GetHashCode(),
            2 => Item2.GetHashCode(),
            _ => throw new ArgumentOutOfRangeException(nameof(i)),
        };
    }
    
}

public readonly record struct Fact<T0, T1, T2, T3>(T0 Item0, T1 Item1, T2 Item2, T3 Item3) : IFact
    where T0 : notnull
    where T1 : notnull
    where T2 : notnull
    where T3 : notnull

{
    public int Arity => 4;

    public int GetHashCode(int i)
    {
        return i switch
        {

            0 => Item0.GetHashCode(),
            1 => Item1.GetHashCode(),
            2 => Item2.GetHashCode(),
            3 => Item3.GetHashCode(),
            _ => throw new ArgumentOutOfRangeException(nameof(i)),
        };
    }
    
}

public readonly record struct Fact<T0, T1, T2, T3, T4>(T0 Item0, T1 Item1, T2 Item2, T3 Item3, T4 Item4) : IFact
    where T0 : notnull
    where T1 : notnull
    where T2 : notnull
    where T3 : notnull
    where T4 : notnull

{
    public int Arity => 5;

    public int GetHashCode(int i)
    {
        return i switch
        {

            0 => Item0.GetHashCode(),
            1 => Item1.GetHashCode(),
            2 => Item2.GetHashCode(),
            3 => Item3.GetHashCode(),
            4 => Item4.GetHashCode(),
            _ => throw new ArgumentOutOfRangeException(nameof(i)),
        };
    }
    
}

public readonly record struct Fact<T0, T1, T2, T3, T4, T5>(T0 Item0, T1 Item1, T2 Item2, T3 Item3, T4 Item4, T5 Item5) : IFact
    where T0 : notnull
    where T1 : notnull
    where T2 : notnull
    where T3 : notnull
    where T4 : notnull
    where T5 : notnull

{
    public int Arity => 6;

    public int GetHashCode(int i)
    {
        return i switch
        {

            0 => Item0.GetHashCode(),
            1 => Item1.GetHashCode(),
            2 => Item2.GetHashCode(),
            3 => Item3.GetHashCode(),
            4 => Item4.GetHashCode(),
            5 => Item5.GetHashCode(),
            _ => throw new ArgumentOutOfRangeException(nameof(i)),
        };
    }
    
}

public readonly record struct Fact<T0, T1, T2, T3, T4, T5, T6>(T0 Item0, T1 Item1, T2 Item2, T3 Item3, T4 Item4, T5 Item5, T6 Item6) : IFact
    where T0 : notnull
    where T1 : notnull
    where T2 : notnull
    where T3 : notnull
    where T4 : notnull
    where T5 : notnull
    where T6 : notnull

{
    public int Arity => 7;

    public int GetHashCode(int i)
    {
        return i switch
        {

            0 => Item0.GetHashCode(),
            1 => Item1.GetHashCode(),
            2 => Item2.GetHashCode(),
            3 => Item3.GetHashCode(),
            4 => Item4.GetHashCode(),
            5 => Item5.GetHashCode(),
            6 => Item6.GetHashCode(),
            _ => throw new ArgumentOutOfRangeException(nameof(i)),
        };
    }
    
}

