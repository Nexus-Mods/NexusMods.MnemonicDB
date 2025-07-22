using NexusMods.HyperDuck;

namespace NexusMods.MnemonicDB.Abstractions;

public interface IQueryEngine
{
    public HyperDuck.Connection Connection { get; }
    
    public LogicalType AttrEnum { get; }
}
