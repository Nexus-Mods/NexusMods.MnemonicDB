using System;

namespace NexusMods.EventSourcing.Abstractions;

public interface IDatomSink
{
    public void Emit(ulong e, ulong a, ulong v, ulong t);
    public void Emit(ulong e, ulong a, long v, ulong t);
    public void Emit(ulong e, ulong a, double v, ulong t);
    public void Emit(ulong e, ulong a, float v, ulong t);
    public void Emit(ulong e, ulong a, string v, ulong t);
    public void Emit(ulong e, ulong a, UInt128 v, ulong t);
    public void Emit(ulong e, ulong a, ReadOnlySpan<byte> v, ulong t);

    public void Emit(ulong e, ulong a, EntityId v, ulong t);
}
