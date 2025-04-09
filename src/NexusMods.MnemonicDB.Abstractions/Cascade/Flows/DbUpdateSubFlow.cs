using NexusMods.Cascade.Abstractions;
using NexusMods.Cascade.Abstractions.Diffs;
using NexusMods.Cascade.Implementation;

namespace NexusMods.MnemonicDB.Abstractions.Cascade.Flows;

internal class DbUpdateSubFlow<TValue>(IReadableAttribute<TValue> attribute, ToDbUpdate parent) : IDiffFlow<EVRow<TValue>>
{
    public ISource<DiffSet<EVRow<TValue>>> ConstructIn(ITopology topology)
    {
        var parentSource = (ToDbUpdate.ToDbUpdateImpl)topology.Intern(parent);
        var subFlow = new DbUpdateSubFlowSource(attribute, parentSource);
        var actualSource = (IDiffSource<EVRow<TValue>>)parentSource.Connect(attribute, subFlow);
        return actualSource;
    }

    public override string ToString()
    {
        return $"DatomsFlow({attribute.Id.Name})";
    }


    internal class DbUpdateSubFlowSource(IReadableAttribute<TValue> attribute, ToDbUpdate.ToDbUpdateImpl parentSource) : ASource<DiffSet<EVRow<TValue>>>, IDiffSource<EVRow<TValue>>, ISink<DbUpdate>
    {
        public override DiffSet<EVRow<TValue>> Current
        {
            get
            {
                var current = parentSource.Current;
                if (current.UpdateType == UpdateType.None)
                    return DiffSet<EVRow<TValue>>.Empty;
                
                var datoms = current.Next.Datoms(attribute);
                var writer = new DiffSetWriter<EVRow<TValue>>();
                var resolver = current.Next.Connection.AttributeResolver;
                foreach (var datom in datoms)
                    writer.Add(new EVRow<TValue>(datom.E, attribute.ReadValue(datom.ValueSpan, datom.Prefix.ValueTag, resolver)));
                
                if (!writer.Build(out var diffSet))
                    return DiffSet<EVRow<TValue>>.Empty;
                
                return diffSet;
            }
        }
        public void OnNext(in DbUpdate current)
        {
            if (current.UpdateType == UpdateType.None)
                return;

            else if (current.UpdateType == UpdateType.Init)
            {
                var datoms = current.Next.Datoms(attribute);
                var writer = new DiffSetWriter<EVRow<TValue>>();
                var resolver = current.Next.Connection.AttributeResolver;
                foreach (var datom in datoms)
                    writer.Add(new EVRow<TValue>(datom.E, attribute.ReadValue(datom.ValueSpan, datom.Prefix.ValueTag, resolver)));

                if (!writer.Build(out var diffSet))
                    return;
                
                Forward(diffSet);
            }
            else if (current.UpdateType == UpdateType.NextTx)
            {
                var writer = new DiffSetWriter<EVRow<TValue>>();
                var resolver = current.Next.Connection.AttributeResolver;
                var attrId = current.Next.AttributeCache.GetAttributeId(attribute.Id);
                foreach (var datom in current.Next.RecentlyAdded)
                {
                    if (datom.A != attrId)
                        continue;
                    
                    writer.Add(new EVRow<TValue>(datom.E, attribute.ReadValue(datom.ValueSpan, datom.Prefix.ValueTag, resolver)));
                }

                if (!writer.Build(out var diffSet))
                    return;
                
                Forward(diffSet);
            }
            else
            {
                var writer = new DiffSetWriter<EVRow<TValue>>();
                var resolver = current.Next.Connection.AttributeResolver;
                
                var oldDatoms = current.Prev.Datoms(attribute);
                foreach (var datom in oldDatoms)
                    writer.Add(new EVRow<TValue>(datom.E, attribute.ReadValue(datom.ValueSpan, datom.Prefix.ValueTag, resolver)), -1);
                
                var newDatoms = current.Prev.Datoms(attribute);
                foreach (var datom in newDatoms) 
                    writer.Add(new EVRow<TValue>(datom.E, attribute.ReadValue(datom.ValueSpan, datom.Prefix.ValueTag, resolver)), 1);
                
                if (!writer.Build(out var diffSet))
                    return;
                
                Forward(diffSet);
            }

        }

        public void OnCompleted()
        {
            CompleteSinks();
        }

        public override string ToString()
        {
            return $"DatomsSource({attribute.Id.Name})";
        }
    }
}
