using System;

namespace NexusMods.EventSourcing.Abstractions;

/// <summary>
/// A sink for datoms, this is used to avoid overhead of using IEnumerable, casting and boxing
/// </summary>
public interface IDatomSinkWithTx
{
    void Datom(ulong e, ulong a, ulong v, ulong tx);
    void Datom(ulong e, ulong a, long v, ulong tx);
    void Datom(ulong e, ulong a, string v, ulong tx);
    void Datom(ulong e, ulong a, bool v, ulong tx);
    void Datom(ulong e, ulong a, double v, ulong tx);
    void Datom(ulong e, ulong a, float v, ulong tx);
    void Datom(ulong e, ulong a, ReadOnlySpan<byte> v, ulong tx);
}
