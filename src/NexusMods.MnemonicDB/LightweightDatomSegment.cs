using System;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Internals;
using NexusMods.MnemonicDB.Abstractions.Traits;

namespace NexusMods.MnemonicDB;

/// <summary>
/// A very lightweight wrapper over a ref datom enumerator
/// </summary>
internal sealed class LightweightDatomSegment<TEnumerator, TDescriptor> : ILightweightDatomSegment, IDisposable
  where TEnumerator : IRefDatomEnumerator
  where TDescriptor : ISliceDescriptor
{
    private TEnumerator _enumerator;
    private TDescriptor _descriptor;
    
    private State _state;

    private enum State : byte
    {
        NotStarted,
        InProgress,
        Finished
    }

    public LightweightDatomSegment(TEnumerator enumerator, TDescriptor descriptor)
    {
        _enumerator = enumerator;
        _descriptor = descriptor;
        _state = State.NotStarted;
    }
    
    public KeyPrefix Prefix => _enumerator.Prefix;
    public ReadOnlySpan<byte> ValueSpan => _enumerator.ValueSpan.Span;
    public ReadOnlySpan<byte> ExtraValueSpan => _enumerator.ExtraValueSpan.Span;
    
    public bool MoveNext()
    {
        if (_state == State.Finished)
            return false;
        _state = State.InProgress;
        return _enumerator.MoveNext(_descriptor);
    }


    public bool FastForwardTo(EntityId eid)
    {
        if (_state == State.Finished)
        {
            return false;
        }

        if (_state == State.NotStarted)
        {
            if (!MoveNext())
            {
                _state = State.Finished;
                _enumerator.Dispose();
                return false; // No items to process
            }
        }
        
        while (_enumerator.E < eid)
        {
            if (!_enumerator.MoveNext(_descriptor))
            {
                _enumerator.Dispose();
                _state = State.Finished;
                return false; // No more items
            }
        }

        return _enumerator.E == eid;
    }

    public void Dispose()
    {
        if (_state != State.Finished) 
            return;
        _enumerator.Dispose();
    }
}
