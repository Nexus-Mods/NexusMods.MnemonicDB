using System;
using System.Collections.Concurrent;
using Clarp.Concurrency;
using NexusMods.Cascade.Abstractions;
using NexusMods.Cascade.Abstractions.Diffs;
using NexusMods.Cascade.TransactionalConnections;

namespace NexusMods.MnemonicDB.Abstractions.Cascade.Flows;

public sealed class ToDbUpdate(IFlow<IDb> upstream) : IFlow<DbUpdate>
{
    private ConcurrentDictionary<IAttribute, IDiffFlow<DbUpdate>> _knownFlows = new();
        
    public ISource<DbUpdate> ConstructIn(ITopology topology)
    {
        var source = topology.Intern(upstream);
        var impl = new ToDbUpdateImpl();
        source.Connect(impl);
        return impl;
    }

    public IDiffFlow<EVRow<TValue>> ForAttribute<TValue>(IReadableAttribute<TValue> attribute)
    {
        throw new NotImplementedException();
    }

    internal class ToDbUpdateImpl : ISource<DbUpdate>, ISink<IDb>
    {
        private Ref<DbUpdate> _state = new(new DbUpdate(null!, null!, UpdateType.None));
        private TxDictionary<IAttribute, ISink<DbUpdate>> _sinks = new();

        public IDisposable Connect(ISink<DbUpdate> sink)
        {
            throw new NotSupportedException(
                "Cannot directly connect to DbUpdate, use the flow construction methods instead");
        }

        public DbUpdate Current
        {
            get
            {
                var current = _state.Value;
                if (current.UpdateType == UpdateType.None)
                    return current;
                return _state.Value with { Prev = null!, UpdateType = UpdateType.Init };
            }
        }

        public void OnNext(in IDb newDb)
        {
            var current = _state.Value;
            var newState = default(DbUpdate);
            if (current.UpdateType == UpdateType.None)
            {
                // initialize the new state
                newState = current with { UpdateType = UpdateType.Init, Next = newDb };
            }
            else if (current.Next.BasisTxId.Value + 1 == newDb.BasisTxId.Value)
            {
                // Transition forward one
                newState = new DbUpdate(UpdateType: UpdateType.NextTx, Next: newDb, Prev: null!);
            }
            else
            {
                // Full reset
                newState = new DbUpdate(current.Next, newDb, UpdateType.RemoveAndAdd);
            }
            _state.Value = newState;
            foreach (var sink in _sinks.Values)
                sink.OnNext(newState);
        }

        public void OnCompleted()
        {
            foreach (var sink in _sinks.Values)
                sink.OnCompleted();
        }

        public ISource<EVRow<TValue>> Connect<TValue>(IReadableAttribute<TValue> attribute, DbUpdateSubFlow<TValue>.DbUpdateSubFlowSource subFlow)
        {
            if (_sinks.TryGetValue(attribute, out var sink))
                return (ISource<EVRow<TValue>>)sink;
            
            _sinks[attribute] = (ISink<DbUpdate>)subFlow;
            return (ISource<EVRow<TValue>>)subFlow;
        }
    }
}
