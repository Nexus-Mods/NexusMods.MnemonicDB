using System.Threading.Tasks;

namespace NexusMods.MnemonicDB.Queryable.Engines;

public interface IEmitter<in T1>
{
    public ValueTask Emit(T1 value);
}
