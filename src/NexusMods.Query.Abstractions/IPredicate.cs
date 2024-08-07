using System.Collections.Generic;

namespace NexusMods.Query.Abstractions;

public interface IPredicate
{
    public void RegisterLVars(HashSet<ILVar> lvars);
}

