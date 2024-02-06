using NexusMods.EventSourcing.Abstractions;

namespace NexusMods.EventSourcing.Sinks;

/// <summary>
/// A sink that loads a single entity from a result set.
/// </summary>
/// <param name="registry"></param>
/// <param name="db"></param>
public struct SingleEntityLoader(IEntityRegistry registry, IDb db) : IResultSetSink
{
    /// <summary>
    /// The entity that was loaded.
    /// </summary>
    public AEntity Entity = null!;

    /// <inheritdoc />
    public void Process<T>(ref T resultSet) where T : IResultSet
    {
        Entity = registry.ReadOne(ref resultSet, db);
    }
}
