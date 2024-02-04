using NexusMods.EventSourcing.Abstractions;

namespace NexusMods.EventSourcing.Tests;

public struct DatomAccumulator : IResultSetSink
{
    public readonly List<(ulong e, ulong a, object v, ulong tx)> Datoms;

    public DatomAccumulator()
    {
        Datoms = new List<(ulong e, ulong a, object v, ulong tx)>();
    }

    public void Process<T>(ref T resultSet) where T : IResultSet
    {
        do
        {
            Datoms.Add((resultSet.EntityId, resultSet.Attribute, resultSet.Value, resultSet.Tx));
        } while (resultSet.Next());
    }
}
