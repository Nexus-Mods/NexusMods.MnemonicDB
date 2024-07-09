using System.Diagnostics.CodeAnalysis;

namespace MnemonicDB.Query;

public interface IEnvironment<TEnv>
where TEnv : IEnvironment<TEnv>
{
    public bool TryGet<T>(Term<T> term, [NotNullWhen(true)] out T value);

    
    /// <summary>
    /// Create a new environment where the term is bound to the value
    /// </summary>
    public TEnv Bind<T>(Term<T> term, T value);

    public TEnv Bind<T>(Term<T> term, LVar other);
}
