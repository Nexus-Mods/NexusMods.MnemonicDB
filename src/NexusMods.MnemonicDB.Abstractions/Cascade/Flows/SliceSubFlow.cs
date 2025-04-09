using System;
using NexusMods.Cascade.Abstractions;
using NexusMods.Cascade.Abstractions.Diffs;
using NexusMods.Cascade.Implementation;
using NexusMods.MnemonicDB.Abstractions.DatomIterators;
using NexusMods.MnemonicDB.Abstractions.Query;

namespace NexusMods.MnemonicDB.Abstractions.Cascade.Flows;

internal class SliceSubFlow : IDiffFlow<Datom>
{
    private readonly SliceFlow _sliceFlow;
    private readonly ISliceDescriptor _sliceDescriptor;

    public SliceSubFlow(SliceFlow sliceFlow, ISliceDescriptor sliceDescriptor)
    {
        _sliceFlow = sliceFlow;
        _sliceDescriptor = sliceDescriptor;
    }
        
    public ISource<DiffSet<Datom>> ConstructIn(ITopology topology)
    {
        var upstreamSource = (SliceFlow.SliceFlowImpl)topology.Intern(_sliceFlow);
        var (from, to, isReverse) = _sliceDescriptor;
        return upstreamSource.SourceForSlice(new SliceDescriptor { From = from, To = to, IsReverse = isReverse });
    }
    
    internal class SliceSubFlowImpl(SliceFlow.SliceFlowImpl parent) : ASource<DiffSet<Datom>>, IDiffSink<Datom>, IDiffSource<Datom>
    {
        public void OnNext(in DiffSet<Datom> value) 
            => Forward(value);

        public void OnCompleted() 
            => CompleteSinks();

        public override DiffSet<Datom> Current => throw new NotImplementedException("TODO");
    }
}
