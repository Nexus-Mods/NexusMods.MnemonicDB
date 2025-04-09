using System;
using Clarp.Concurrency;
using NexusMods.Cascade.Abstractions;
using NexusMods.Cascade.Abstractions.Diffs;
using NexusMods.Cascade.Implementation;
using NexusMods.Cascade.TransactionalConnections;
using NexusMods.MnemonicDB.Abstractions.DatomIterators;
using NexusMods.MnemonicDB.Abstractions.Query;

namespace NexusMods.MnemonicDB.Abstractions.Cascade.Flows;

public sealed class SliceFlow : IDiffFlow<Datom>
{
    private readonly IFlow<IDb> _upstream;

    public SliceFlow(IFlow<IDb> upstream)
    {
        _upstream = upstream;
    }
    
    /// <summary>
    /// Get a sub-flow for the datoms in the Db that match the given slice.
    /// </summary>
    public IDiffFlow<Datom> GetFlow<TSlice>(TSlice slice) where TSlice : ISliceDescriptor
    {
        return new SliceSubFlow(this, slice);
        
    }
    
    public ISource<DiffSet<Datom>> ConstructIn(ITopology topology)
    {
        throw new System.NotImplementedException();
    }

    internal class SliceFlowImpl : ISink<IDb>, IDiffSource<Datom>
    {
        private TxDictionary<SliceDescriptor, IDiffSink<Datom>> _sinks = new();
        private readonly Ref<(IDb Db, bool HavePrevious)> _previousDb = new((null!, false));


        public
            void OnNext(in IDb value)
        {
            var previous = _previousDb.Value;

            // Initial setup case
            if (!previous.HavePrevious)
            {
                foreach (var (slice, sink) in _sinks)
                {
                    var datoms = value.Snapshot.Datoms(slice);
                    var writer = new DiffSetWriter<Datom>();
                    foreach (var datom in datoms)
                        writer.Add(datom);
                    
                    if (!writer.Build(out var diffSet))
                        continue;
                    sink.OnNext(diffSet);
                }
            }
            // Normal case of moving one tx forward.
            else if (previous.Db.BasisTxId.Value + 1 == value.BasisTxId.Value)
            {
                var txLog = value.RecentlyAdded;
                foreach (var (slice, sink) in _sinks)
                {
                    var writer = new DiffSetWriter<Datom>();
                    foreach (var datom in txLog)
                    {
                        if (!slice.Includes(datom))
                            continue;
                        writer.Add(datom);
                    }

                    if (!writer.Build(out var diffSet))
                        continue;
                    sink.OnNext(diffSet);
                }
                _previousDb.Value = (value, true);
            }
            // Slow case where we're completely resetting the state
            else
            {
                foreach (var (slice, sink) in _sinks)
                {
                    var writer = new DiffSetWriter<Datom>();
                    var prevDatoms = previous.Db.Snapshot.Datoms(slice);
                 
                    // Retract all the previous datoms
                    foreach (var datom in prevDatoms)
                        writer.Add(datom, -1);

                    // Add all the new datoms
                    var newDatoms = value.Snapshot.Datoms(slice);
                    foreach (var datom in newDatoms)
                        writer.Add(datom);
                    
                    if (!writer.Build(out var diffSet))
                        continue;
                    sink.OnNext(diffSet);
                }
            }
            
            _previousDb.Value = (value, true);
        }

        public void OnCompleted()
        {
            foreach (var (_ , sink) in _sinks)
                sink.OnCompleted();
        }

        public IDiffSource<Datom> SourceForSlice(SliceDescriptor slice)
        {
            if (_sinks.TryGetValue(slice, out var sink))
                return (IDiffSource<Datom>)sink;
            
            var subSource = new SliceSubFlow.SliceSubFlowImpl(this);
            _sinks[slice] = subSource;
            return subSource;
                
        }

        public IDisposable Connect(ISink<Datom> sink)
        {
            throw new NotSupportedException("Cannot connect directly to a slice flow, use SourceForSlice instead");
        }

        public IDisposable Connect(ISink<DiffSet<Datom>> sink)
        {

            throw new NotSupportedException("Cannot connect directly to a slice flow, use SourceForSlice instead");
        }

        public DiffSet<Datom> Current => throw new NotImplementedException();
    }
}
