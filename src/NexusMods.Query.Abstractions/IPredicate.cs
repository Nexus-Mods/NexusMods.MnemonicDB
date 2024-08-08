using System;
using System.Collections.Generic;
using NexusMods.Query.Abstractions.Engines;
using NexusMods.Query.Abstractions.Engines.TopDownLazy;

namespace NexusMods.Query.Abstractions;

public interface IPredicate
{
    public void RegisterLVars(HashSet<ILVar> lvars);
    public Func<IEnumerable<ILVarBox[]>, IEnumerable<ILVarBox[]>> MakeLazy(Context context);
}

