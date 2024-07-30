namespace NexusMods.MnemonicDB.Abstractions;

/// <summary>
/// Interface for a transaction analyzer. These can be injected via DI and they will then be fed each database transaction
/// to analyze and produce a result.
/// </summary>
public interface IAnalyzer
{
    /// <summary>
    /// Analyze the database and produce a result.
    /// </summary>
    public object Analyze(IDb db);
}

/// <summary>
/// Typed version of <see cref="IAnalyzer"/> that specifies the type of the result.
/// </summary>
public interface IAnalyzer<out T> : IAnalyzer
{

}

