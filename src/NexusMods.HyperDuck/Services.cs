using Microsoft.Extensions.DependencyInjection;
using NexusMods.HyperDuck.Adaptor;
using NexusMods.HyperDuck.Adaptor.Impls;
using NexusMods.HyperDuck.Adaptor.Impls.ResultAdaptors;
using NexusMods.HyperDuck.Adaptor.Impls.RowAdaptors;
using NexusMods.HyperDuck.Adaptor.Impls.ValueAdaptor;

namespace NexusMods.HyperDuck;

public static class Services
{
    public static IServiceCollection AddAdapters(this IServiceCollection s)
    {
        s.AddSingleton<IRegistry, Registry>();
        s.AddSingleton<IResultAdaptorFactory, ListAdaptorFactory>();
        s.AddSingleton<IRowAdaptorFactory, SingleColumnAdaptorFactory>();
        s.AddSingleton<IValueAdaptorFactory, UnmanagedValueAdaptorFactory>();
        s.AddSingleton<IRowAdaptorFactory, TupleAdaptorFactory>();
        s.AddSingleton<IValueAdaptorFactory, StringAdaptorFactory>();
        s.AddSingleton<IValueAdaptorFactory, RelativePathAdaptorFactory>();
        return s;
    }
}
