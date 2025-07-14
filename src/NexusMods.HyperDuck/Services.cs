using Microsoft.Extensions.DependencyInjection;
using NexusMods.HyperDuck.Adaptor;
using NexusMods.HyperDuck.Adaptor.Impls;

namespace NexusMods.HyperDuck;

public static class Services
{
    public static IServiceCollection AddAdapters(this IServiceCollection s)
    {
        s.AddSingleton<Registry>();
        s.AddSingleton<Builder>();
        s.AddSingleton<IConverter, ListChunk>();
        s.AddSingleton<IConverter, ScalarRow>();
        s.AddSingleton<IConverter, TupleRowConverter>();
        s.AddSingleton<IConverter, StringValueConverter>();
        s.AddSingleton<IConverter, ListValueConverter>();
        return s;
    }
}
