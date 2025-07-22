using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Hashing.xxHash3;
using NexusMods.HyperDuck;
using NexusMods.HyperDuck.Adaptor;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.BuiltInEntities;
using NexusMods.MnemonicDB.TestModel;
using NexusMods.Paths;

namespace NexusMods.MnemonicDB.Tests;

public static class Startup
{
    
    
    public static IServiceCollection ConfigureServices(this IServiceCollection services)
    {
        VerifierSettings.AddExtraSettings(_ =>
        {
            _.Converters.Add(new ToStringConverter<TxId>());
            _.Converters.Add(new ToStringConverter<Hash>());
            _.Converters.Add(new ToStringConverter<RelativePath>());
            _.Converters.Add(new TupleWriter());
            
        });
        
        return services
            .AddAttributeDefinitionModel()
            .AddAdapters()
            .AddTransactionModel()
            .AddTestModel()
            .AddSingleton<TemporaryFileManager>()
            .AddFileSystem()
            .AddAdapters()
            .AddSingleton<IQueryEngine, QueryEngine>()
            .AddLogging(builder => builder.AddConsole());
    }
}

class ToStringConverter<T> : WriteOnlyJsonConverter
{
    public override bool CanConvert(Type type)
    {
        return type.IsAssignableTo(typeof(T));
    }
    public override void Write(VerifyJsonWriter writer, object value)
    {
        writer.WriteValue(value.ToString());
    }
}

class TupleWriter : WriteOnlyJsonConverter
{
    public override bool CanConvert(Type type)
    {
        return type.IsAssignableTo(typeof(ITuple));
    }

    public override void Write(VerifyJsonWriter writer, object value)
    {
        writer.WriteStartArray();
        var tup = (ITuple)value;
        for (var idx = 0; idx < tup.Length; idx++) 
        {
            writer.Serialize(tup[idx]!);
        }
        writer.WriteEndArray();
    }
}
