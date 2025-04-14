using NexusMods.Cascade;
using NexusMods.Cascade.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Cascade.Flows;

namespace NexusMods.MnemonicDB.Abstractions.Cascade;

public static class FlowExtensions
{
    public static Flow<DbUpdate> ToDbUpdate(this IFlow<IDb> upstream)
    {
        return new FlowDescription
        {
            UpstreamFlows = [upstream.AsFlow()],
            InitFn = () => default(DbUpdate),
            Reducers = [ReducerFn],
            DebugInfo = DebugInfo.Create("ToDbUpdate", "", 0),
        };
        
        static (Node, object?) ReducerFn(Node node, int tag, object value)
        {
            var prev = node.UserState as IDb;
            var newNode = node with { UserState = value };
            var newDb = (IDb)value;
            DbUpdate update;

            if (prev is null)
            {
                update = new DbUpdate(null!, newDb, UpdateType.Init);
            }
            else if (prev.BasisTxId.Value + 1 == newDb.BasisTxId.Value)
            {
                update = new DbUpdate(null!, newDb, UpdateType.NextTx);
            }
            else
            {
                update = new DbUpdate(prev, newDb, UpdateType.RemoveAndAdd);
            }
            
            return (newNode, update);
        }

    }

    public static DiffFlow<EVRow<T>> QueryAll<T>(this IReadableAttribute<T> attribute)
    {
        return new FlowDescription
        {
            UpstreamFlows = [Query.Updates.AsFlow()],
            Reducers = [ReducerFn],
            DebugInfo = DebugInfo.Create($"All: {attribute.Id}", "", 0),
        };
        
        (Node, object?) ReducerFn(Node node, int tag, object value)
        {
            var current = (DbUpdate)value;

            var diffSet = new DiffSet<EVRow<T>>();
            
            if (current.UpdateType == UpdateType.None)
                return (node, null!);
            else if (current.UpdateType == UpdateType.Init)
            {
                var datoms = current.Next.Datoms(attribute);
                var resolver = current.Next.Connection.AttributeResolver;
                foreach (var datom in datoms)
                    diffSet.Add(new EVRow<T>(datom.E, attribute.ReadValue(datom.ValueSpan, datom.Prefix.ValueTag, resolver)), 1);
            }
            else if (current.UpdateType == UpdateType.NextTx)
            {
                var resolver = current.Next.Connection.AttributeResolver;
                var attrId = current.Next.AttributeCache.GetAttributeId(attribute.Id);
                foreach (var datom in current.Next.RecentlyAdded)
                {
                    if (datom.A != attrId)
                        continue;
                    
                    diffSet.Add(new EVRow<T>(datom.E, attribute.ReadValue(datom.ValueSpan, datom.Prefix.ValueTag, resolver)), 1);
                }
            }
            else
            {
                var resolver = current.Next.Connection.AttributeResolver;
                
                var oldDatoms = current.Prev.Datoms(attribute);
                foreach (var datom in oldDatoms)
                    diffSet.Add(new EVRow<T>(datom.E, attribute.ReadValue(datom.ValueSpan, datom.Prefix.ValueTag, resolver)), -1);
                
                var newDatoms = current.Prev.Datoms(attribute);
                foreach (var datom in newDatoms) 
                    diffSet.Add(new EVRow<T>(datom.E, attribute.ReadValue(datom.ValueSpan, datom.Prefix.ValueTag, resolver)), 1);
            }
            
            if (diffSet.Count == 0)
                return (node, null!);
            
            return (node, diffSet);
        }
        
    }
    
}
