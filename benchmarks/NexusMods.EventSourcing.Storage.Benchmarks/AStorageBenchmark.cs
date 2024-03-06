using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Storage.Serializers;
using NexusMods.EventSourcing.Storage.Tests;

namespace NexusMods.EventSourcing.Storage.Benchmarks;

public class AStorageBenchmark
{
    private readonly IServiceProvider _services;
    protected readonly AttributeRegistry _registry;

    public AStorageBenchmark()
    {
        var host = Host.CreateDefaultBuilder()
            .ConfigureServices(s =>
            {
                s.AddEventSourcingStorage()
                    .AddSingleton<AttributeRegistry>()
                    .AddSingleton<IKvStore, InMemoryKvStore>()
                    .AddAttribute<TestAttributes.FileHash>()
                    .AddAttribute<TestAttributes.FileName>();
            })
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddConsole();
            })
            .Build();
        _services = host.Services;

        _registry = _services.GetRequiredService<AttributeRegistry>();

        _registry.Populate(new[]
        {
            new DbAttribute(Symbol.Intern<TestAttributes.FileHash>(), AttributeId.From(10), Symbol.Intern<UInt64Serializer>()),
            new DbAttribute(Symbol.Intern<TestAttributes.FileName>(), AttributeId.From(11), Symbol.Intern<StringSerializer>())
        });
    }
}
