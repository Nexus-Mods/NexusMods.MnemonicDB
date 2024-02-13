using Microsoft.Extensions.DependencyInjection;
using NexusMods.EventSourcing.TestModel.Model;

namespace NexusMods.EventSourcing.TestModel;

public static class Services
{
    public static IServiceCollection AddTestModel(this IServiceCollection services) =>
        services.AddModModel()
                .AddLoadoutModel();
}
