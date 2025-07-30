using System.Threading.Tasks;
using NexusMods.HyperDuck;

namespace NexusMods.MnemonicDB.Abstractions;

public interface IQueryEngine
{
    public HyperDuck.DuckDB DuckDb { get; }
    
    public LogicalType AttrEnum { get; }
}
