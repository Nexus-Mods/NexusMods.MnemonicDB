using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.Query.Abstractions.Engines.Abstract;

public static class ConstantNodes
{
    /// <summary>
    /// Database variable, used in queries against MnemonicDB
    /// </summary>
    public static Variable<IDb> Db { get; } = Variable<IDb>.New("Db");
    
}
