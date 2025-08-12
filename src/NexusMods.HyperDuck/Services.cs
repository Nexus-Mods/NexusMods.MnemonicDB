using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.HyperDuck.Adaptor;
using NexusMods.HyperDuck.Adaptor.Impls;
using NexusMods.HyperDuck.Adaptor.Impls.ResultAdaptors;
using NexusMods.HyperDuck.Adaptor.Impls.RowAdaptors;
using NexusMods.HyperDuck.Adaptor.Impls.ValueAdaptor;
using NexusMods.HyperDuck.BindingConverters;

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
        s.AddSingleton<IValueAdaptorFactory, ListValueAdaptorFactory>();
        s.AddSingleton<IResultAdaptorFactory, ObservableListAdaptorFactory>();
        s.AddSingleton<IResultAdaptorFactory, SourceCacheAdaptorFactory>();
        s.AddSingleton<IValueAdaptorFactory, DateTimeOffsetAdaptorFactory>();
        
        s.AddSingleton<IBindingConverter, GenericBindingConverter>();
        
        return s;
    }
}
