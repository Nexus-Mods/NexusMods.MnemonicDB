using System;
using NexusMods.EventSourcing.Abstractions.Internals;

namespace NexusMods.EventSourcing.Abstractions.DatomIterators;

/// <summary>
///     Iterates over the datoms while the attribute is equal to the given value
/// </summary>
public class WhileA<TParent>(AttributeId a, TParent parent) : IIterator
    where TParent : IIterator
{
    /// <inheritdoc />
    public bool Valid => parent.Valid && this.CurrentKeyPrefix().A == a;

    /// <inheritdoc />
    public void Next()
    {
        parent.Next();
    }

    /// <inheritdoc />
    public void Prev()
    {
        parent.Prev();
    }

    /// <inheritdoc />
    public ReadOnlySpan<byte> Current => parent.Current;

    /// <inheritdoc />
    public IAttributeRegistry Registry => parent.Registry;
}

/// <summary>
///     Iterates over the datoms while the attribute is equal to the given value
/// </summary>
public class WhileE<TParent>(EntityId e, TParent parent) : IIterator
    where TParent : IIterator
{
    /// <inheritdoc />
    public bool Valid => parent.Valid && this.CurrentKeyPrefix().E == e;

    /// <inheritdoc />
    public void Next()
    {
        parent.Next();
    }

    /// <inheritdoc />
    public void Prev()
    {
        parent.Prev();
    }

    /// <inheritdoc />
    public ReadOnlySpan<byte> Current => parent.Current;

    /// <inheritdoc />
    public IAttributeRegistry Registry => parent.Registry;
}

/// <summary>
///     Iterates over the datoms while the attribute is equal to the given value
/// </summary>
public class WhileTx<TParent>(TxId txId, TParent parent) : IIterator
    where TParent : IIterator
{
    /// <inheritdoc />
    public bool Valid => parent.Valid && this.CurrentKeyPrefix().T == txId;

    /// <inheritdoc />
    public void Next()
    {
        parent.Next();
    }

    /// <inheritdoc />
    public void Prev()
    {
        parent.Prev();
    }

    /// <inheritdoc />
    public ReadOnlySpan<byte> Current => parent.Current;

    /// <inheritdoc />
    public IAttributeRegistry Registry => parent.Registry;
}

/// <summary>
///     Iterates over the datoms while the value (unmanaged) is equal to the given value
/// </summary>
public class WhileUnmanagedV<TParent, TValue>(TValue v, TParent parent) : IIterator
    where TParent : IIterator
    where TValue : unmanaged, IEquatable<TValue>
{
    /// <inheritdoc />
    public bool Valid => parent.Valid && parent.CurrentValue<TParent, TValue>().Equals(v);

    /// <inheritdoc />
    public void Next()
    {
        parent.Next();
    }

    /// <inheritdoc />
    public void Prev()
    {
        parent.Prev();
    }

    /// <inheritdoc />
    public ReadOnlySpan<byte> Current => parent.Current;

    /// <inheritdoc />
    public IAttributeRegistry Registry => parent.Registry;
}
