using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace NexusMods.MnemonicDB.IntervalTree;

public sealed record ImmutableIntervalTree<TKey, TValue> 
    where TKey : IComparable<TKey>
{
    private ImmutableList<AugmentedInterval> _intervals = ImmutableList<AugmentedInterval>.Empty;
    private BuiltTree? _builtTree = null;

    private record BuiltTree(AugmentedInterval[] Tree, int Count, int Height);
    
    private static readonly BuiltTree EmptyTree = new([], 0, 0);
    
    /// <summary>
    /// An empty interval tree.
    /// </summary>
    public static readonly ImmutableIntervalTree<TKey, TValue> Empty = new();
    
    private ImmutableIntervalTree()
    {
        _intervals = ImmutableList<AugmentedInterval>.Empty;
    }
    
    /// <summary>
    /// Add a new interval to the tree, the tree will be rebuilt on the next query.
    /// </summary>
    public ImmutableIntervalTree<TKey, TValue> Add(TKey from, TKey to, TValue value)
    {
        var newIntervals = _intervals.Add(new AugmentedInterval { From = from, To = to, Max = to, Value = value });
        return this with { _intervals = newIntervals, _builtTree = null };
    }
    
    /// <summary>
    /// Query the tree for all intervals that contain the target value, may build the tree if it has not been built yet.
    /// </summary>
    public IEnumerable<TValue> Query(TKey target)
    {
        var builtTree = _builtTree ??= BuildTree();

        if (builtTree.Count == 0)
            return [];

        List<TValue>? results = null;

        Span<int> stack = stackalloc int[2 * builtTree.Height];
        stack[0] = 0;
        stack[1] = builtTree.Count - 1;
        var stackIndex = 1;

        while (stackIndex > 0)
        {
            var max = stack[stackIndex--];
            var min = stack[stackIndex--];

            var span = max - min;
            if (span < 6) // At small subtree sizes a linear scan is faster
            {
                for (var i = min; i <= max; i++)
                {
                    var interval = builtTree.Tree[i];

                    var compareFrom = target.CompareTo(interval.From);
                    if (compareFrom < 0)
                        break;

                    var compareTo = target.CompareTo(interval.To);
                    if (compareTo > 0)
                        continue;

                    results ??= new List<TValue>();
                    results.Add(interval.Value);
                }
            }
            else
            {
                var center = min + (max - min + 1) / 2;
                var interval = builtTree.Tree[center];

                var compareMax = target.CompareTo(interval.Max);
                if (compareMax > 0) continue; // target larger than Max, bail
                
                // search left
                stack[++stackIndex] = min;
                stack[++stackIndex] = center - 1;

                // check current node
                var compareFrom = target.CompareTo(interval.From);

                if (compareFrom < 0) continue; // target smaller than From, bail
                else
                {
                    var compareTo = target.CompareTo(interval.To);
                    if (compareTo <= 0)
                    {
                        results ??= [];
                        results.Add(interval.Value);
                    }
                }

                // search right
                stack[++stackIndex] = center + 1;
                stack[++stackIndex] = max;
            }
        }

        return results ?? Enumerable.Empty<TValue>();
    }


    
    private BuiltTree BuildTree()
    {
        if (_intervals.Count == 0)
            return EmptyTree;

        var toSort = _intervals.ToArray();
        Array.Sort(toSort);
        var treeHeight = (int)Math.Log(toSort.Length, 2) + 1;
        UpdateMaxRec(0, toSort.Length - 1, 0);
        
        return new BuiltTree(toSort, toSort.Length, treeHeight);
        
        TKey UpdateMaxRec(int min, int max, int recursionLevel)
        {
            if (recursionLevel++ > 100)
                throw new Exception($"Excessive recursion detected, aborting to prevent stack overflow. Please check thread safety.");

            var center = min + (max - min + 1) / 2;

            var interval = _intervals[center];

            if (max - min <= 0)
            {
                // set max to 'To'
                toSort[center] = interval with
                {
                    Max = interval.To
                };
                return interval.To;
            }
            else
            {
                // find max between children and own 'To'
                var maxValue = interval.To;

                var left = UpdateMaxRec(min, center - 1, recursionLevel);
                var right = center < max ?
                    UpdateMaxRec(center + 1, max, recursionLevel) :
                    maxValue;

                if (left.CompareTo(maxValue) > 0)
                    maxValue = left;
                if (right.CompareTo(maxValue) > 0)
                    maxValue = right;

                // update max
                toSort[center] = interval with
                {
                    Max = maxValue
                };
                return maxValue;
            }
        }
    }
    
    internal readonly record struct AugmentedInterval : IComparable<AugmentedInterval>
    {
        public TKey From { get; init; }
        public TKey To { get; init; }
        public TKey Max { get; init; }
        public TValue Value { get; init; }

        public int CompareTo(AugmentedInterval other)
        {
            var fromComparison = From.CompareTo(other.From);
            if (fromComparison != 0)
                return fromComparison;
            return To.CompareTo(other.To);
        }
    }

    /// <summary>
    /// Remove the interval from the tree, the tree will be rebuilt on the next query.
    /// </summary>
    public ImmutableIntervalTree<TKey, TValue> Remove(TValue value)
    {
        return RemoveWhere(static (toFind, v) => v!.Equals(toFind), value);
    }

    /// <summary>
    /// Remove all intervals that match the predicate, the tree will be rebuilt on the next query.
    /// </summary>
    public ImmutableIntervalTree<TKey, TValue> RemoveWhere<TState>(Func<TValue, TState, bool> predicate, TState state)
    {
        var intervals = _intervals;
        var i = 0;
        while (i < intervals.Count)
        {
            var interval = intervals[i];
            if (predicate(interval.Value, state))
            {
                intervals = intervals.RemoveAt(i);
            }
            else
            {
                i++;
            }
        }

        // No change, return self
        if (ReferenceEquals(intervals, _intervals))
            return this;
        
        return this with { _intervals = intervals, _builtTree = null };
    }
}
