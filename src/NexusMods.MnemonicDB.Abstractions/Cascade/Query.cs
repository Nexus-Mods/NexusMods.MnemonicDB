using NexusMods.Cascade.Abstractions.Diffs;
using NexusMods.Cascade.Implementation;
using NexusMods.MnemonicDB.Abstractions.Cascade.Flows;

namespace NexusMods.MnemonicDB.Abstractions.Cascade;

public static class Query
{
    public static readonly Inlet<IDb> Db = new();
    
    public static readonly ToDbUpdate Updates = new(Db);

    public static IDiffFlow<EVRow<T>> QueryAll<T>(this IReadableAttribute<T> attr)
    {
        return Updates.ForAttribute(attr);
    }
    
}
