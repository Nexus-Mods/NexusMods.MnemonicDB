using System.Threading.Tasks;
using NexusMods.HyperDuck;

namespace NexusMods.MnemonicDB.Abstractions;

public interface IQueryEngine
{
    public HyperDuck.Database Database { get; }
    
    public LogicalType AttrEnum { get; }
}
