using System;
using System.Linq;

namespace NexusMods.MnemonicDB.QueryableParser.Abstractions;

public interface IDataSource
{
    public Type ElementType { get; }
}

public interface IDataSource<T> : IDataSource
{
    public Type ElementType => typeof(T);
}

