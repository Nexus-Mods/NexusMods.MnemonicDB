using System;
using NexusMods.Hashing.xxHash3;

namespace NexusMods.HyperDuck;

public struct HashedQuery : IEquatable<HashedQuery>
{
    private readonly int _hash;

    public HashedQuery(byte[] sql)
    {
        Sql = sql;
        _hash = (int)sql.xxHash3().Value;
    }

    public byte[] Sql { get; set; }

    public bool Equals(HashedQuery other)
    {
        if (other._hash != _hash)
            return false;
        return other.Sql.AsSpan().SequenceEqual(Sql.AsSpan());
    }

    public override bool Equals(object? obj)
    {
        return obj is HashedQuery other && Equals(other);
    }

    public override int GetHashCode()
    {
        return _hash;
    }

    public static HashedQuery Create(string sql)
    {
        var bytes = new byte[sql.Length + 1];
        for (var i = 0; i < sql.Length; i++)
            bytes[i] = (byte)sql[i];
        bytes[sql.Length] = 0;
        return new HashedQuery(bytes);
    }
}
