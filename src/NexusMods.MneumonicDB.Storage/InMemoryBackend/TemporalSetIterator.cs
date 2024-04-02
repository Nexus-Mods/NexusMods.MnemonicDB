using System;
using NexusMods.MneumonicDB.Abstractions.DatomIterators;
using NexusMods.MneumonicDB.Abstractions.Internals;

namespace NexusMods.MneumonicDB.Storage.InMemoryBackend;

public class TemporalSetIterator<TComparator>(SortedSetIterator a, SortedSetIterator b, AttributeRegistry registry) : IDatomSource, IIterator
    where TComparator : IDatomComparator<AttributeRegistry>
{
    public void Dispose()
    {

    }

    public IIterator SeekLast()
    {
        a.SeekLast();
        b.SeekLast();
        UpdateState();
        return this;
    }

    public IIterator Seek(ReadOnlySpan<byte> datom)
    {
        ((ISeekableIterator)a).Seek(datom);
        ((ISeekableIterator)b).Seek(datom);
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

    private State _state = State.Invalid;
    private void UpdateState()
    {
        var aValid = a.Valid;
        var bValid = b.Valid;

        if (aValid && bValid)
        {
            var cmp = TComparator.Compare(registry, a.Current, b.Current);
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

    public IIterator SeekStart()
    {
        a.SeekStart();
        b.SeekStart();
        UpdateState();
        return this;
    }

    public bool Valid => _state != State.Invalid;
    public ReadOnlySpan<byte> Current => _state switch
    {
        State.A => a.Current,
        State.B => b.Current,
        State.Both => a.Current,
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
