using NexusMods.HyperDuck;

namespace NexusMods.MnemonicDB.Abstractions;

public interface IQueryEngine
{
    /// <summary>
    /// Register a table function with the engine 
    /// </summary>
    public void Register(ATableFunction tableFunction);

    /// <summary>
    /// Run the query and return the results
    /// </summary>
    public T Query<T>(string sql);
}
