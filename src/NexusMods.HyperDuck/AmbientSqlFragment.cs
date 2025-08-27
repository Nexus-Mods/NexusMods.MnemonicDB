namespace NexusMods.HyperDuck;

/// <summary>
/// A DI record for SQL statements that will be loaded into the query context on startup
/// </summary>
/// <param name="Source"></param>
/// <param name="SQL"></param>
public record AmbientSqlFragment(string Source, string SQL);
