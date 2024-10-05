using System;
using System.Collections.Generic;
using System.Linq;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.MnemonicDB.Dataflow.Operators;

/// <summary>
/// Performs a transformation on the input values. Any duplicate values are merged into a single output
/// value with the sum of their counts.
/// </summary>
public class Select<TInput, TOutput, TTimestamp> : AUnaryOperator<TInput, TOutput, TTimestamp>
    where TInput : notnull
    where TOutput : notnull
    where TTimestamp : IComparable<TTimestamp>
{
    private readonly Func<TInput,TOutput> _fn;
    private readonly Dictionary<TOutput,int> _counts;

    public Select(Func<TInput, TOutput> fn) : base(Symbol.Intern("Select"))
    {
        _fn = fn;
        _counts = new Dictionary<TOutput, int>();
    }
    
    protected override void Process(ReadOnlySpan<(TInput, int)> values, TTimestamp timestamp)
    {
        foreach (var value in values)
        {
            var output = _fn(value.Item1);
            var count = _counts.GetValueOrDefault(output, 0);
            _counts[output] = count + value.Item2;
        }
        Send(_counts.Select(kv => (kv.Key, kv.Value)).ToArray(), timestamp);
        _counts.Clear();
    }
}
