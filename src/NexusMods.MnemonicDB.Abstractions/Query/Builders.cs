using NexusMods.Cascade;
using NexusMods.Cascade.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Models;
using NexusMods.MnemonicDB.Abstractions.Query.Stages;

namespace NexusMods.MnemonicDB.Abstractions.Query;

public static class Builders
{
    public static IQuery<T> All<T>() where T : IQueryableEntity<T>
    {
        return new QueryableEntityAll<T>(new UpstreamConnection(QueryInlets.Db, QueryInlets.Db.Output));
    }
}
