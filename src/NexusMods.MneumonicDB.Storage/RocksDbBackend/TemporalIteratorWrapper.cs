using System;
using NexusMods.MneumonicDB.Abstractions.DatomIterators;
using NexusMods.MneumonicDB.Abstractions.Internals;
using RocksDbSharp;

namespace NexusMods.MneumonicDB.Storage.RocksDbBackend;

public class TemporalIteratorWrapper<TComparator>(Iterator a, Iterator b, AttributeRegistry registry) : IDatomSource, IIterator
    where TComparator : IDatomComparator<AttributeRegistry>
{
    public void Dispose()
    {
        a.Dispose();
        b.Dispose();
    }

    public IIterator SeekLast()
    {
        a.SeekToLast();
        b.SeekToLast();
        UpdateState();
        return this;
    }

    public IIterator Seek(ReadOnlySpan<byte> datom)
    {
        a.Seek(datom);
        b.Seek(datom);
        UpdateState();
        return this;
    }

    public IIterator SeekStart()
    {
        a.SeekToFirst();
        b.SeekToFirst();
        UpdateState();
        return this;
    }

    private enum State
    {
        Invalid,
        A,
        B,
        Both
    }

    private void UpdateState()
    {
        var aValid = a.Valid();
        var bValid = b.Valid();

        if (aValid && bValid)
        {
            var cmp = TComparator.Compare(registry, a.GetKeySpan(), b.GetKeySpan());
            _state = cmp switch
            {
                < 0 => State.A,
                > 0 => State.B,
                _ => State.Both
            };
        }
        else if (aValid)
        {
            _state = State.A;
        }
        else if (bValid)
        {
            _state = State.B;
        }
        else
        {
            _state = State.Invalid;
        }
    }

    private State _state = State.Invalid;
    public bool Valid => _state != State.Invalid;
    public ReadOnlySpan<byte> Current => _state switch
    {
        State.A => a.GetKeySpan(),
        State.B => b.GetKeySpan(),
        State.Both => a.GetKeySpan(),
        _ => throw new InvalidOperationException()
    };
    public IAttributeRegistry Registry => registry;
    public void Next()
    {
        switch (_state)
        {
            case State.A:
                a.Next();
                break;
            case State.B:
                b.Next();
                break;
            case State.Both:
                a.Next();
                b.Next();
                break;
            default:
                throw new InvalidOperationException();
        }
        UpdateState();
    }

    public void Prev()
    {
        switch (_state)
        {
            case State.A:
                a.Prev();
                break;
            case State.B:
                b.Prev();
                break;
            case State.Both:
                a.Prev();
                b.Prev();
                break;
            default:
                throw new InvalidOperationException();
        }
        UpdateState();
    }
}
