using System;

namespace NexusMods.HyperDuck.Adaptor.Impls;

public static class Helpers
{
    public static bool AssignNotEq<T>(ref T a, T b)
    {
        bool eq;
        if (a is IEquatable<T> casted)
            eq = casted.Equals(b);
        else
        {
            eq = Equals(a, b);
        }
        a = b;
        return !eq;
    }
}
