using Microsoft.Extensions.DependencyInjection;
using NexusMods.HyperDuck.Adaptor;
using NexusMods.HyperDuck.Adaptor.Impls;
using NexusMods.HyperDuck.Adaptor.Impls.ResultAdaptors;

namespace NexusMods.HyperDuck;

public static class Services
{
    public static IServiceCollection AddAdapters(this IServiceCollection s)
    {
        s.AddSingleton<IRegistry, Registry>();
        s.AddSingleton<IResultAdaptorFactory, ListAdaptorFactory>();
        
        return s;
    }
}
