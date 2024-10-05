using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.MnemonicDB.Dataflow;

public abstract class AUnaryOperator<TInputType, TOutputType, TTimestamp> : AOperator 
where TTimestamp : IComparable<TTimestamp> where TOutputType : notnull
{
    private readonly Input _input;
    private readonly Output _output;
    private static readonly Symbol InputName = Symbol.Intern("Unary/Input");
    private static readonly Symbol OutputName = Symbol.Intern("Unary/Output");

    protected AUnaryOperator(Symbol name) : base(name)
    {
        _input = new Input(this);
        _output = new Output(this);
    }

    protected abstract void Process(ReadOnlySpan<(TInputType, int)> values, TTimestamp timestamp);
    
    /// <summary>
    /// Called by Process to send the processed values to the output
    /// </summary>
    /// <param name="values"></param>
    /// <param name="timestamp"></param>
    /// <exception cref="NotImplementedException"></exception>
    protected virtual void Send(ReadOnlySpan<(TOutputType, int)> values, TTimestamp timestamp)
    {
        _output.Send(values, timestamp);
    }
    
    private class Input(AUnaryOperator<TInputType, TOutputType, TTimestamp> parent) : IInput<TInputType, TTimestamp>
    {
        public Symbol Name => InputName;
        public Type ValueType => typeof(TInputType);
        public Type TimestampType => typeof(TTimestamp);
        
        private Dictionary<TTimestamp, List<(TInputType, int)>> _values = new();

        public void OnRecv(ReadOnlySpan<(TInputType, int)> values, TTimestamp timestamp)
        {
            if (!_values.TryGetValue(timestamp, out var list))
            {
                list = [];
                _values.Add(timestamp, list);
            }
            list.AddRange(values);
        }

        public void OnNotify(TTimestamp timestamp)
        {
            if (!_values.TryGetValue(timestamp, out var list)) 
                return;
            
            parent.Process(CollectionsMarshal.AsSpan(list), timestamp);
            _values.Remove(timestamp);
        }
    }
    
    private class Output(AUnaryOperator<TInputType, TOutputType, TTimestamp> parent) : IOutput<TOutputType, TTimestamp>
    {
        public Symbol Name => OutputName;
        public Type ValueType => typeof(TOutputType);
        public Type TimestampType => typeof(TTimestamp);
        
        private HashSet<IInput<TOutputType, TTimestamp>> _inputs = [];

        public void Attach(IInput<TOutputType, TTimestamp> input)
        {
            _inputs.Add(input);
        }
        
        protected internal void Send(ReadOnlySpan<(TOutputType, int)> values, TTimestamp timestamp)
        {
            foreach (var input in _inputs)
            {
                input.OnRecv(values, timestamp);
            }
        }
    }
}
