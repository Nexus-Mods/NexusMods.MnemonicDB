using System;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Internals;

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

    public LightweightDatomSegment(TEnumerator enumerator, TDescriptor descriptor)
    {
        _enumerator = enumerator;
        _descriptor = descriptor;
    }
    
    public KeyPrefix KeyPrefix => _enumerator.KeyPrefix;
    public ReadOnlySpan<byte> ValueSpan => _enumerator.ValueSpan.Span;
    public ReadOnlySpan<byte> ExtraValueSpan => _enumerator.ExtraValueSpan.Span;
    
    public bool MoveNext()
    {
        return _enumerator.MoveNext(_descriptor);
    }

    public void Dispose()
    {
        _enumerator.Dispose();
    }
}
