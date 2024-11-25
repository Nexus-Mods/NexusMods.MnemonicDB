using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using DynamicData;

namespace NexusMods.MnemonicDB.QueryEngine.Tables;

public static class Extensions
{
    
    public static ITable HashJoin(this IMaterializedTable left, ITable right)
    {
        var newColumns = left.Columns.Concat(right.Columns).Distinct().ToArray();
        var newTable = new AppendableTable(newColumns);

        var hashLeft = new Dictionary<int, List<int>>();
        var joinColumns = left.Columns.Intersect(right.Columns).ToArray();
        
        var leftColumns = GC.AllocateUninitializedArray<IMaterializedColumn>(joinColumns.Length);
        foreach (var join in joinColumns)
        {
            leftColumns[Array.IndexOf(left.Columns, join)] = (IMaterializedColumn)left[join];
        }
        
        for (var rowIdx = 0; rowIdx < left.Count; rowIdx++)
        {
            var hash = 0;
            foreach (var column in leftColumns)
            {
                hash = HashCode.Combine(hash, column.GetHashCode(rowIdx));
            }
            
            ref var r = ref CollectionsMarshal.GetValueRefOrNullRef(hashLeft, hash);
            if (Unsafe.IsNullRef(ref r))
                hashLeft.Add(hash, [rowIdx]);
            else
                r.Add(rowIdx);
        }

        var rowEnum = right.EnumerateRows();
        var rowColumns = GC.AllocateUninitializedArray<int>(joinColumns.Length);
        for (var joinIdx = 0; joinIdx < joinColumns.Length; joinIdx++)
        {
            rowColumns[joinIdx] = right.Columns.IndexOf(joinColumns[joinIdx]);
        }
        
        var newJoinColumns = joinColumns.Select(c => newTable[c]).ToArray();
        var newRightColumns = right.Columns.Select(c => newTable[c]).ToArray();
        var newLeftColumns = left.Columns.Select(c => newTable[c]).ToArray();
        
        var leftJoinColumns = joinColumns.Select(c => left[c]).ToArray();
        var rightJoinColumns = joinColumns.Select(c => right[c]).ToArray();
        
        while (rowEnum.MoveNext())
        {
            var hash = 0;
            foreach (var column in rowColumns) 
                hash = HashCode.Combine(hash, rowEnum.GetHashCode(column));
            
            if (!hashLeft.TryGetValue(hash, out var leftRows))
                continue;

            foreach (var leftRow in leftRows)
            {
                for (int joinIdx = 0; joinIdx < joinColumns.Length; joinIdx++)
                {
                    if (rowEnum.Equal(leftColumn, 
                }
                
            }
        }

        throw new NotImplementedException();
        return newTable;
    }
    
}
