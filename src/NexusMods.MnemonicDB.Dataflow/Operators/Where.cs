using System;
using System.Collections.Generic;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.MnemonicDB.Dataflow.Operators;

/// <summary>
/// Only forwards on the values that satisfy the given predicate
/// </summary>
public class Where<TInput, TTimestamp> : AUnaryOperator<TInput, TInput, TTimestamp>
    where TInput : notnull
    where TTimestamp : IComparable<TTimestamp>
{
    private readonly List<(TInput, int)> _accumulator;
    private readonly Func<TInput,bool> _predicate;

    public Where(Func<TInput, bool> predicate) : base(Symbol.Intern("Where"))
    {
        _predicate = predicate;
        _accumulator = [];
    }

    protected override void Process(ReadOnlySpan<(TInput, int)> values, TTimestamp timestamp)
    {
        foreach (var value in values)
        {
            if (_predicate(value.Item1))
                _accumulator.Add(value);
        }
        Send(_accumulator.ToArray(), timestamp);
        _accumulator.Clear();
    }
}
