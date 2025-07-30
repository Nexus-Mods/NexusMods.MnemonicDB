using System.Runtime.CompilerServices;
using NexusMods.HyperDuck.Adaptor;

namespace NexusMods.HyperDuck;

public class Query
{
    public static CompiledQuery<TResult> Compile<TResult>(string query)
    {
        return new CompiledQuery<TResult>(query);
    }

    public static CompiledQuery<TResult, TArg1> Compile<TResult, TArg1>(string query)
    {
        return new CompiledQuery<TResult, TArg1>(query);
    }
}

public abstract class ACompiledQuery
{
    
}

public abstract class ACompiledQuery<TResult> : ACompiledQuery
{
    private IResultAdaptor<TResult>? _resultAdaptor;
    
    public ACompiledQuery(string query)
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

public class CompiledQuery<TResult> : ACompiledQuery<TResult>
{
    public CompiledQuery(string query) : base(query)
    {
    }
}


public class CompiledQuery<TResult, TArg1> : ACompiledQuery<TResult>
{
    public CompiledQuery(string query) : base(query)
    {
    }
}
