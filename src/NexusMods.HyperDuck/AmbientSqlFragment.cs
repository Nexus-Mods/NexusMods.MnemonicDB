namespace NexusMods.HyperDuck;

/// <summary>
/// A DI record for SQL statements that will be loaded into the query context on startup
/// </summary>
/// <param name="Source">A friendly identifier (usually the filename without extension).</param>
/// <param name="SQL">The SQL content to load.</param>
/// <param name="Namespace">The declared ambient SQL namespace from the file.</param>
/// <param name="Requires">Ambient SQL namespaces that must be loaded before this fragment.</param>
public record AmbientSqlFragment(string Source, string SQL, string Namespace, string[] Requires);
