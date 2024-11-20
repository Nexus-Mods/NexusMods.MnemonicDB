using System;
using System.Collections.Generic;

namespace NexusMods.MnemonicDB.QueryEngine;

/// <summary>
/// An abstract column of data in a table
/// </summary>
public interface IColumn
{
    /// <summary>
    /// The data type of the column
    /// </summary>
    public Type Type { get; }
}

/// <summary>
/// A column of data in a table
/// </summary>
public interface IColumn<out T> : IColumn, IReadOnlyCollection<T>
{
    /// <summary>
    /// Get the value of the column at the given row
    /// </summary>
    public T this[int row] { get; }
}

