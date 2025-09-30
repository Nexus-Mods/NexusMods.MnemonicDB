using System.Linq;
using NexusMods.HyperDuck;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Storage;

namespace NexusMods.MnemonicDB.QueryFunctions;

public class ActiveConnectionsTable : ATableFunction
{
    private readonly QueryEngine _engine;

    public ActiveConnectionsTable(QueryEngine engine)
    {
        _engine = engine;
    }
    
    protected override void Setup(RegistrationInfo info)
    {
        info.SetName("mnemonicdb_active_connections");
    }

    protected override void Execute(FunctionInfo functionInfo)
    {
        var chunk = functionInfo.Chunk;
        var globalObjectIds = chunk[0].GetData<ushort>();
        var names = chunk[1];
        var lastTxIds = chunk[2].GetData<ulong>();
        var paths = chunk[3];
        var isReadOnlys = chunk[4].GetData<bool>();

        var localData = functionInfo.GetInitInfo<LocalData>();
        int row = 0;
        foreach (var conn in localData.Connections)
        {
            var db = conn.Connection.Db;
            var lastTxId = db.BasisTxId;
            var settings = ((DatomStore)conn.Connection.DatomStore).Settings;
            
            globalObjectIds[row] = conn.UniqueId;
            names.WriteUtf16((ulong)row, conn.Name);
            lastTxIds[row] = lastTxId.Value;
            paths.WriteUtf16((ulong)row, settings.Path?.ToString() ?? "NONE");
            isReadOnlys[row] = ((Connection)conn.Connection).ReadOnlyMode;
            row += 1;
        }
        chunk.Size = (ulong)row;
        localData.Connections = localData.Connections.Skip(row).ToArray();
    }

    protected override object? Init(InitInfo initInfo, InitData initData)
    {
        return new LocalData()
        {
            Connections = _engine.Connections
        };
    }

    private class LocalData
    {
        public QueryEngine.ActiveConnection[] Connections { get; set; } = [];
    }

    protected override void Bind(BindInfo info)
    {
        info.AddColumn<ushort>("GlobalObjectId");
        info.AddColumn<string>("Name");
        info.AddColumn<ulong>("LastTxId");
        info.AddColumn<string>("Path");
        info.AddColumn<bool>("IsReadOnly");
    }
}
