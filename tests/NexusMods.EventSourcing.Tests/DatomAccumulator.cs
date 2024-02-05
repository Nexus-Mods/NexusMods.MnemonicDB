using NexusMods.EventSourcing.Abstractions;

namespace NexusMods.EventSourcing.Tests;

public struct DatomAccumulator : IResultSetSink
{
    public readonly List<(ulong e, ulong a, object v, ulong tx)> Datoms;
    private readonly ulong _lowEntityId;
    private readonly ulong _highEntityId;

    public DatomAccumulator(bool limitToUserSpace = false)
    {
        _lowEntityId = limitToUserSpace ? Ids.MinId(IdSpace.Entity) : ulong.MaxValue;
        _highEntityId = limitToUserSpace ? Ids.MaxId(IdSpace.Entity) : ulong.MaxValue;
        Datoms = new List<(ulong e, ulong a, object v, ulong tx)>();
    }


    public void Process<T>(ref T resultSet) where T : IResultSet
    {
        do
        {
            if (resultSet.EntityId < _lowEntityId || resultSet.EntityId > _highEntityId)
                continue;
            Datoms.Add((resultSet.EntityId, resultSet.Attribute, resultSet.Value, resultSet.Tx));
        } while (resultSet.Next());
    }
}
