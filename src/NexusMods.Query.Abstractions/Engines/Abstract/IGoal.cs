using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace NexusMods.Query.Abstractions.Engines.Abstract;

public interface IGoal
{ 
    Environment.Execute Emit(EnvironmentDefinition environment, Environment.Execute innerExpr);
}
