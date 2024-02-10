using Microsoft.Extensions.DependencyInjection;
using NexusMods.EventSourcing.Abstractions;

namespace NexusMods.EventSourcing.TestModel;

public static class Mod
{
    internal static IServiceCollection AddMod(this IServiceCollection services)
    {
        services.AddAttribute<Name>()
                .AddAttribute<Description>()
                .AddAttribute<Enabled>();
        return services;
    }

    public class Name() : ScalarAttribute<Name, string>("F49CE692-77B9-451D-B8A3-71645EBBCAE0");
    public class Description() : ScalarAttribute<Description, string>("EB0ED697-7825-42BD-9F50-EE70BE056639");
    public class Enabled() : ScalarAttribute<Enabled, bool>("1D156CF6-8A13-422B-B54E-4D05AE3C8442");
}
