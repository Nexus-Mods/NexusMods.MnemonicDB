using System;
using System.Threading;

namespace MnemonicDB.Query;

public readonly struct LVar : IEquatable<LVar>
{
    private static ulong _nextId = 1;
    private readonly ulong _id = 0;
    
    public bool Valid => _id != 0;

    public LVar()
    {
        _id = 0;
    }
    
    private LVar(ulong id)
    {
        _id = id;
    }

    public static LVar New()
    {
        return new LVar(Interlocked.Increment(ref _nextId));
    }

    public bool Equals(LVar other)
    {
        if (_id == 0 || other._id == 0)
            return false;
        return _id == other._id;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((LVar)obj);
    }

    public override int GetHashCode()
    {
        return _id.GetHashCode();
    }
}
