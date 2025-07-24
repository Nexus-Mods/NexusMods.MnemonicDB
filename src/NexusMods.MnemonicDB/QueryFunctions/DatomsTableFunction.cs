using System;
using System.Collections.Generic;
using System.Linq;
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

    private enum UnionOrdering : byte
    {
        Bool = 0,
        UInt64, // 64 bit unsigned integer
        String,
        Reference, // Reference to another MDB entity
        End = Reference,
    }

    public DatomsTableFunction(IConnection conn, IEnumerable<IAttribute> attrs, IQueryEngine queryEngine, string prefix = "mdb")
    {
        _prefix = prefix;
        _conn = conn;
        _engine = null;
        _attrs = attrs.OrderBy(attr => attr.Id.Id).ToArray();
        _attrsByShortName = attrs.ToDictionary(a => $"{a.Id.Namespace.Split(".").Last()}/{a.Id.Name}");
        var cache = conn.AttributeCache;
        _attrIdToEnum = new ushort[cache.MaxAttrId + 1];
        _queryEngine = (QueryEngine)queryEngine;
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
            if (state.History)
                isRetract[row] = iterator.KeyPrefix.IsRetract ? (byte)1 : (byte)0;
            
            if (vVec.IsValid)
            {
                switch (lowLevelType)
                {
                    case ValueTag.UInt64:
                        vData.CastFast<byte, ulong>()[row] = UInt64Serializer.Read(iterator.ValueSpan);
                        break;

                    case ValueTag.Utf8:
                        vVec.WriteUtf8((ulong)row, iterator.ValueSpan);
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
        IDb db = _conn.Db;
        if (history)
        {
            db = _conn.History();
        }
        
        LocalBindData state;
        if (!aParam.IsNull)
        {
            var attrToFind = aParam.GetVarChar();
            if (!_attrsByShortName.TryGetValue(attrToFind, out var attr))
                throw new Exception($"Attribute '{attrToFind}' not found");

            state = new LocalBindData
            {
                Mode = ScanMode.SingleAttribute,
                Attribute = attr,
            };
        }
        else
        {
            state = new LocalBindData
            {
                Mode = ScanMode.AllAttributes,
                Attribute = null
            };
        }
        
        info.SetBindInfo(state);
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
    }

    protected override object? Init(InitInfo initInfo, InitData initData)
    {
        var bindData = initInfo.GetBindData<LocalBindData>();
        var db = _conn.Db;
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
        public bool History = false;
        public required ScanMode Mode;
        public required IAttribute? Attribute;
    }

    private class LocalInitData
    {
        public required ILightweightDatomSegment Segment;
    }
}
