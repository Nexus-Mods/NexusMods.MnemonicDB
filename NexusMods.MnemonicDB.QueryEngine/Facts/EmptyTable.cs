using System;

namespace NexusMods.MnemonicDB.QueryEngine.Facts;

public class EmptyTable : ITable
{
    public LVar[] Columns => [];
    public Type FactType => throw new NotSupportedException();

    /// <summary>
    /// The singleton instance of the empty table
    /// </summary>
    public static readonly EmptyTable Instance = new();
}
