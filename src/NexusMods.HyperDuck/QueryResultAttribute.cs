using System;
using JetBrains.Annotations;

namespace NexusMods.HyperDuck;

[PublicAPI]
[AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public class QueryResultAttribute : Attribute { }

