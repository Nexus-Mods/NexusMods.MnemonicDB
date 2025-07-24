using System.Runtime.CompilerServices;
using NexusMods.HyperDuck.Adaptor;

namespace NexusMods.HyperDuck;

public class Query
{
    public static CompiledQuery<TResult> Compile<TResult>(string query)
    {
        return new CompiledQuery<TResult>(query);
    }
}

public class CompiledQuery
{
    
}

public class CompiledQuery<TResult> : CompiledQuery
{
    private IResultAdaptor<TResult>? _resultAdaptor;
    public CompiledQuery(string query)
    {
        Sql = query;
    }
    
    public string Sql { get; }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IResultAdaptor<TResult> Adaptor(Result result, IRegistry dbRegistry)
    {
        _resultAdaptor ??= dbRegistry.GetAdaptor<TResult>(result);
        return _resultAdaptor;
    }

    public override string ToString()
    {
        return $"CompiledQuery: {Sql}";
    }
}
