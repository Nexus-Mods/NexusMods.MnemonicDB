using NexusMods.EventSourcing.Abstractions;

namespace NexusMods.EventSourcing.Tests;

public class DatomAccumulator : IDatomSinkWithTx
{
    public readonly List<(ulong e, ulong a, object v, ulong tx)> Datoms = new();
    public void Datom(ulong e, ulong a, ulong v, ulong tx)
    {
        Datoms.Add((e, a, v, tx));
    }

    public void Datom(ulong e, ulong a, long v, ulong tx)
    {
        Datoms.Add((e, a, v, tx));
    }

    public void Datom(ulong e, ulong a, string v, ulong tx)
    {
        Datoms.Add((e, a, v, tx));
    }

    public void Datom(ulong e, ulong a, bool v, ulong tx)
    {
        Datoms.Add((e, a, v, tx));
    }

    public void Datom(ulong e, ulong a, double v, ulong tx)
    {
        Datoms.Add((e, a, v, tx));
    }

    public void Datom(ulong e, ulong a, float v, ulong tx)
    {
        Datoms.Add((e, a, v, tx));
    }

    public void Datom(ulong e, ulong a, ReadOnlySpan<byte> v, ulong tx)
    {
        Datoms.Add((e, a, v.ToArray(), tx));
    }
}
