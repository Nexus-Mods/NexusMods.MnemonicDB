﻿using System.IO.Hashing;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Models;
using NexusMods.MnemonicDB.Storage;
using NexusMods.MnemonicDB.Storage.RocksDbBackend;
using NexusMods.MnemonicDB.TestModel.Helpers;
using NexusMods.Hashing.xxHash64;
using NexusMods.MnemonicDB.Abstractions.DatomIterators;
using NexusMods.MnemonicDB.TestModel;
using NexusMods.MnemonicDB.TestModel.Analyzers;
using NexusMods.Paths;
using Xunit.Sdk;
using File = NexusMods.MnemonicDB.TestModel.File;

namespace NexusMods.MnemonicDB.Tests;

public class AMnemonicDBTest : IDisposable
{
    private readonly IAttribute[] _attributes;
    protected readonly IServiceProvider Provider;
    protected AttributeCache AttributeCache;
    private Backend _backend;

    private DatomStore _store;
    protected IConnection Connection;
    protected ILogger Logger;
    private readonly IAnalyzer[] _analyzers;


    protected AMnemonicDBTest(IServiceProvider provider)
    {
        Provider = provider;
        _attributes = provider.GetRequiredService<IEnumerable<IAttribute>>().ToArray();

        AttributeCache = new AttributeCache();

        Config = new DatomStoreSettings
        {
            Path = FileSystem.Shared.GetKnownPath(KnownPath.EntryDirectory)
                .Combine("tests_MnemonicDB" + Guid.NewGuid())
        };
        _backend = new Backend(AttributeCache);

        _store = new DatomStore(provider.GetRequiredService<ILogger<DatomStore>>(), AttributeCache, Config, _backend);

        _analyzers =
        [
            new DatomCountAnalyzer(),
            new AttributesAnalyzer()
        ];
        
        Connection = new Connection(provider.GetRequiredService<ILogger<Connection>>(), _store, provider, _analyzers);

        Logger = provider.GetRequiredService<ILogger<AMnemonicDBTest>>();
    }

    protected DatomStoreSettings Config { get; set; }

    protected SettingsTask VerifyModel<T>(T model)
    where T : IEnumerable<IReadDatom>
    {
        return VerifyTable(model);
    }

    protected SettingsTask VerifyModel<T>(IEnumerable<T> models)
    where T : IEnumerable<IReadDatom>
    {
        return VerifyTable(models.SelectMany(e => e));
    }

    protected SettingsTask VerifyTable(IEnumerable<IReadDatom> datoms)
    {
        return Verify(ToTable(datoms, AttributeCache));
    }
    
    public static string ToTable(IEnumerable<IReadDatom> datoms, AttributeCache cache)
    {
        string TruncateOrPad(string val, int length)
        {
            if (val.Length > length)
            {
                var midPoint = length / 2;
                return (val[..(midPoint - 2)] + "..." + val[^(midPoint - 2)..]).PadRight(length);
            }

            return val.PadRight(length);
        }

        var dateTimeCount = 0;

        var sb = new StringBuilder();
        foreach (var datom in datoms)
        {
            var isRetract = datom.IsRetract;

            var symColumn = TruncateOrPad(datom.A.Id.Name, 24);
            var attrId = cache.GetAttributeId(datom.A.Id).Value.ToString("X4");

            sb.Append(isRetract ? "-" : "+");
            sb.Append(" | ");
            sb.Append(datom.E.Value.ToString("X16"));
            sb.Append(" | ");
            sb.Append($"({attrId}) {symColumn}");
            sb.Append(" | ");



            switch (datom.ObjectValue)
            {
                case EntityId eid:
                    sb.Append(eid.Value.ToString("X16").PadRight(48));
                    break;
                case ulong ul:
                    sb.Append(ul.ToString("X16").PadRight(48));
                    break;
                case byte[] byteArray:
                    var code = XxHash3.HashToUInt64(byteArray);
                    var hash = code.ToString("X16");
                    sb.Append($"Blob 0x{hash} {byteArray.Length} bytes".PadRight(48));
                    break;
                case DateTime dateTime:
                    sb.Append($"DateTime : {dateTimeCount++}".PadRight(48));
                    break;
                default:
                    sb.Append(TruncateOrPad(datom.ObjectValue.ToString()!, 48));
                    break;
            }

            sb.Append(" | ");
            sb.Append(datom.T.Value.ToString("X16"));

            sb.AppendLine();
        }

        return sb.ToString();
    }

    protected async Task<Loadout.ReadOnly> InsertExampleData()
    {

        var tx = Connection.BeginTransaction();
        var loadout = new Loadout.New(tx)
        {
            Name = "Test Loadout"
        };
        List<Mod.New> mods = new();

        foreach (var modName in new[] { "Mod1", "Mod2", "Mod3" })
        {
            var mod = new Mod.New(tx)
            {
                Name = modName,
                Source = new Uri("http://somesite.com/" + modName),
                LoadoutId = loadout
            };

            var idx = 0;
            foreach (var file in new[] { "File1", "File2", "File3" })
            {
                _ = new File.New(tx)
                {
                    Path = file,
                    ModId = mod,
                    Size = Size.From((ulong)idx),
                    Hash = Hash.From((ulong)(0xDEADBEEF + idx))
                };
                idx += 1;
            }
            mods.Add(mod);
        }

        var txResult = await tx.Commit();
        var loadoutWritten = txResult.Remap(loadout);

        var tx2 = Connection.BeginTransaction();
        foreach (var mod in loadoutWritten.Mods)
        {
            tx2.Add(txResult[mod.Id], Mod.Name, mod.Name + " - Updated");
        }
        await tx2.Commit();

        return Loadout.Load(Connection.Db, loadoutWritten.Id);
    }

    public void Dispose()
    {
        _store.Dispose();
    }


    protected async Task RestartDatomStore()
    {
        _store.Dispose();
        _backend.Dispose();

        GC.Collect();

        _backend = new Backend(AttributeCache);
        AttributeCache = new AttributeCache();
        _store = new DatomStore(Provider.GetRequiredService<ILogger<DatomStore>>(), AttributeCache, Config, _backend);

        Connection = new Connection(Provider.GetRequiredService<ILogger<Connection>>(), _store, Provider, _analyzers);
    }

}
