using System;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.QueryEngine.Facts;

namespace NexusMods.MnemonicDB.QueryEngine.Ops;

public interface IOp
{
    public ITable Execute(IDb db);
    
    public LVar[] LVars { get; }
    
    public Type FactType { get; }
}
