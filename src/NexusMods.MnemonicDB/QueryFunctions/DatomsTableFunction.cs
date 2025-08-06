using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;
using NexusMods.HyperDuck;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.Query;
using NexusMods.MnemonicDB.Abstractions.ValueSerializers;
using Reloaded.Memory.Extensions;

namespace NexusMods.MnemonicDB.QueryFunctions;

public class DatomsTableFunction : ATableFunction
{
    private readonly IAttribute[] _attrs;
    private IQueryEngine? _engine;
    private readonly IConnection _conn;
    private readonly string _prefix;
    private readonly ushort[] _attrIdToEnum;
    private readonly Dictionary<string, IAttribute> _attrsByShortName;
    private readonly QueryEngine _queryEngine;
    private readonly IDisposable _txWatcher;

    private enum UnionOrdering : byte
    {
        Bool = 0,
        UInt64, // 64 bit unsigned integer
        String,
        Reference, // Reference to another MDB entity
        End = Reference,
    }

    public DatomsTableFunction(IConnection conn, IEnumerable<IAttribute> attrs, IQueryEngine queryEngine,
        IObservable<HashSet<IAttribute>> observer, string prefix = "mdb")
    {
        _prefix = prefix;
        _conn = conn;
        _engine = null;
        _attrs = attrs.OrderBy(attr => attr.Id.Id).ToArray();
        _attrsByShortName = attrs.ToDictionary(a => $"{a.Id.Namespace.Split(".").Last()}/{a.Id.Name}");
        var cache = conn.AttributeCache;
        _attrIdToEnum = new ushort[cache.MaxAttrId + 1];
        _queryEngine = (QueryEngine)queryEngine;
        _txWatcher = observer.Subscribe(_ => Revise());
        for (int i = 0; i < attrs.Count(); i++)
        {
            var attr = _attrs[i];
            _attrIdToEnum[cache.GetAttributeId(attr.Id).Value] = (ushort)i;
        }
    }
    
    protected override void Setup(RegistrationInfo info)
    {
        info.SetName($"{_prefix}_Datoms");
        info.AddNamedParameter<string>("A");
        info.AddNamedParameter<bool>("History");
        info.AddNamedParameter<ulong>("Db");
        info.SupportsPredicatePushdown();
    }

    protected override void Execute(FunctionInfo functionInfo)
    {
        var state = functionInfo.GetBindInfo<LocalBindData>();
        var localInitData = functionInfo.GetInitInfo<LocalInitData>();
        if (state.Mode == ScanMode.AllAttributes)
            ExecuteAllAttributes(functionInfo, state, localInitData);
        else
        {
            ExecuteOneAttribute(functionInfo, state, localInitData);
        }
    }

    private void ExecuteOneAttribute(FunctionInfo functionInfo, LocalBindData state, LocalInitData initData)
    {

        var chunk = functionInfo.Chunk;
        var eVec = functionInfo.GetWritableVector(0).GetData<ulong>();
        var vVec = functionInfo.GetWritableVector(1);
        var vData = vVec.GetData();
        var tVec = functionInfo.GetWritableVector(2).GetData<ulong>();
        var lowLevelType = state.Attribute!.LowLevelType;
        
        Span<byte> isRetract = Span<byte>.Empty;
        
        if (state.History)
            isRetract = functionInfo.GetWritableVector(3).GetData<byte>();
        
        var iterator = initData.Segment;
        var row = 0;
        while (iterator.MoveNext())
        {
            if (!eVec.IsEmpty)
                eVec[row] = iterator.KeyPrefix.E.Value;
            if (!tVec.IsEmpty) 
                tVec[row] = iterator.KeyPrefix.T.Value;
            if (state.History && !isRetract.IsEmpty)
                isRetract[row] = iterator.KeyPrefix.IsRetract ? (byte)1 : (byte)0;
            
            if (vVec.IsValid)
            {
                switch (lowLevelType)
                {
                    case ValueTag.UInt8:
                        vData.CastFast<byte, byte>()[row] = iterator.ValueSpan[0];
                        break;
                    case ValueTag.UInt16:
                        vData.CastFast<byte, ushort>()[row] = iterator.ValueSpan.CastFast<byte, ushort>()[0];
                        break;
                    case ValueTag.UInt32:
                        vData.CastFast<byte, uint>()[row] = iterator.ValueSpan.CastFast<byte, uint>()[0];
                        break;
                    case ValueTag.UInt64:
                        vData.CastFast<byte, ulong>()[row] = UInt64Serializer.Read(iterator.ValueSpan);
                        break;
                    case ValueTag.UInt128:
                        vData.CastFast<byte, UInt128>()[row] = UInt128Serializer.Read(iterator.ValueSpan);
                        break;
                    case ValueTag.Int16:
                        vData.CastFast<byte, short>()[row] = iterator.ValueSpan.CastFast<byte, short>()[0];
                        break;
                    case ValueTag.Int32:
                        vData.CastFast<byte, int>()[row] = iterator.ValueSpan.CastFast<byte, int>()[0];
                        break;
                    case ValueTag.Int64:
                        vData.CastFast<byte, long>()[row] = iterator.ValueSpan.CastFast<byte, long>()[0];
                        break;
                    case ValueTag.Int128:
                        vData.CastFast<byte, Int128>()[row] = Int128Serializer.Read(iterator.ValueSpan);
                        break;
                    case ValueTag.Float32:
                        vData.CastFast<byte, float>()[row] = iterator.ValueSpan.CastFast<byte, float>()[0];
                        break;
                    case ValueTag.Float64:
                        vData.CastFast<byte, double>()[row] = iterator.ValueSpan.CastFast<byte, double>()[0];
                        break;
                    case ValueTag.Ascii:
                        vVec.WriteUtf8((ulong)row, iterator.ValueSpan);
                        break;
                    case ValueTag.Utf8:
                        vVec.WriteUtf8((ulong)row, iterator.ValueSpan);
                        break;
                    case ValueTag.Utf8Insensitive:
                        vVec.WriteUtf8((ulong)row, iterator.ValueSpan);
                        break;
                    case ValueTag.Reference:
                        vData.CastFast<byte, ulong>()[row] = iterator.ValueSpan.CastFast<byte, ulong>()[0];
                        break;
                    default:
                        throw new NotImplementedException($"Not implemented for {lowLevelType}");
                }
            }

            row++;
            if (row >= eVec.Length)
                break;
        }
        
        chunk.Size = (ulong)row;
    }

    private void ExecuteAllAttributes(FunctionInfo functionInfo, LocalBindData state, LocalInitData initData)
    {
        
        var chunk = functionInfo.Chunk;
        var eVec = functionInfo.GetWritableVector(0).GetData<ulong>();
        var aVec = functionInfo.GetWritableVector(1).GetData<byte>();
        var vVec = functionInfo.GetWritableVector(2);
        var tVec = functionInfo.GetWritableVector(3).GetData<ulong>();
        var vTagVec = functionInfo.GetWritableVector(4).GetData<byte>();
        var historyVector = functionInfo.GetWritableVector(5).GetData<byte>();

        var iterator = initData.Segment;
        int row = 0;
        int width = _queryEngine.AttrEnumWidth;
        while (iterator.MoveNext())
        {
            if (!eVec.IsEmpty)
                eVec[row] = iterator.KeyPrefix.E.Value;
            if (!aVec.IsEmpty)
            {
                if (width == 1)
                    aVec[row] = (byte)_attrIdToEnum[iterator.KeyPrefix.A.Value];
                if (width == 2)
                    aVec.CastFast<byte, ushort>()[row] = _attrIdToEnum[iterator.KeyPrefix.A.Value];
            }

            if (!tVec.IsEmpty) 
                tVec[row] = iterator.KeyPrefix.T.Value;
            
            if (vVec.IsValid)
                vVec.WriteBlob((ulong)row, iterator.ValueSpan);

            if (!vTagVec.IsEmpty)
                vTagVec[row] = (byte)iterator.KeyPrefix.ValueTag;
            if (!historyVector.IsEmpty)
                historyVector[row] = (byte)(iterator.KeyPrefix.IsRetract ? 1 : 0);
                
            row++;
            if (row >= eVec.Length)
                break;
        }
        chunk.Size = (ulong)row;
    }

    protected override void Bind(BindInfo info)
    {
        var aParam = info.GetParameter("A");
        var historyParam = info.GetParameter("History");
        var history = !historyParam.IsNull && historyParam.GetBool();
        
        var dbParam = info.GetParameter("Db");
        IConnection connection;
        TxId asOf;
        if (!dbParam.IsNull)
        {
            var db = dbParam.GetUInt64();
            var dbAndAsOf = dbParam.GetUInt64();
            asOf = TxId.From(PartitionId.Transactions.MakeEntityId(dbAndAsOf >> 16).Value);
            connection = _conn;
        }
        else
        {
            asOf = TxId.MinValue;
            connection = _conn;
        }
        
        LocalBindData state;
        if (!aParam.IsNull)
        {
            var attrToFind = aParam.GetVarChar();
            if (!_attrsByShortName.TryGetValue(attrToFind, out var attr))
                throw new Exception($"Attribute '{attrToFind}' not found");

            state = new LocalBindData
            {
                Connection = connection,
                AsOf = asOf,
                Mode = ScanMode.SingleAttribute,
                Attribute = attr,
            };
        }
        else
        {
            state = new LocalBindData
            {
                Connection = connection,
                AsOf = asOf,
                Mode = ScanMode.AllAttributes,
                Attribute = null
            };
        }

        if (state.Mode == ScanMode.SingleAttribute)
        {
            info.AddColumn<ulong>("E");
            info.AddColumn("V", state.Attribute!.LowLevelType.DuckDbType());
            info.AddColumn<ulong>("T");
        }
        else
        {
            info.AddColumn<ulong>("E");
            info.AddColumn("A", _queryEngine.AttrEnum);
            info.AddColumn<byte[]>("V");
            info.AddColumn<ulong>("T");
            info.AddColumn("ValueTag", _queryEngine.ValueTagEnum);
        }
        
        state.History = history;
        if (history)
            info.AddColumn<bool>("IsRetract");
        info.SetBindInfo(state);
    }

    protected override object? Init(InitInfo initInfo, InitData initData)
    {
        var bindData = initInfo.GetBindData<LocalBindData>();
        IDb db;
        if (bindData.AsOf == TxId.MinValue)
            db = _conn.Db;
        else
            db = _conn.AsOf(bindData.AsOf);
        if (bindData.History)
            db = _conn.History();
        
        if (bindData.Mode == ScanMode.SingleAttribute)
        {
            return new LocalInitData
            {
                Segment = db.LightweightDatoms(SliceDescriptor.Create(bindData.Attribute!, _conn.AttributeCache))
            };
        }
        else
        {
            return new LocalInitData
            {
                Segment = db.LightweightDatoms(SliceDescriptor.AllEntities(PartitionId.Entity))
            };
        }
        
    }

    private enum ScanMode
    {
        SingleAttribute,
        AllAttributes,
    }

    private class LocalBindData
    {
        public required IConnection Connection;
        public required TxId AsOf;
        public bool History = false;
        public required ScanMode Mode;
        public required IAttribute? Attribute;
    }

    private class LocalInitData
    {
        public required ILightweightDatomSegment Segment;
    }
}
