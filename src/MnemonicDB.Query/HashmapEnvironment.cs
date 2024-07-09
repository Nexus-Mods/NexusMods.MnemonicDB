using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace MnemonicDB.Query;

public class HashmapEnvironment : IEnvironment<HashmapEnvironment>
{
    private ImmutableDictionary<LVar, object> _bindings = ImmutableDictionary<LVar, object>.Empty;
    public bool TryGet<T>(Term<T> term, [NotNullWhen(true)] out T value)
    {
        if (term.Grounded)
        {
            value = term.Constant!;
            return true;
        }
        
        if (_bindings.TryGetValue(term.LVar, out var obj))
        {
            value = (T)obj;
            return true;
        }
        value = default!;
        return false;
    }

    public HashmapEnvironment Bind<T>(Term<T> term, T value)
    {
        return new HashmapEnvironment
        {
            _bindings = _bindings.Add(term.LVar, value!)
        };
    }

    public HashmapEnvironment Bind<T>(Term<T> term, LVar other)
    {
        return new HashmapEnvironment()
        {
            _bindings = _bindings.Add(term.LVar, other)
        };
    }
}
