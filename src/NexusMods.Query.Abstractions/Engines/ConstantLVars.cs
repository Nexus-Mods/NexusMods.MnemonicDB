using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.Query.Abstractions.Engines;

public class ConstantLVars
{
    public static LVar<IDb> Db { get; } = new("Db");
}
