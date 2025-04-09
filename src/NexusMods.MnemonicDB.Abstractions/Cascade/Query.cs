using System;
using NexusMods.Cascade;
using NexusMods.Cascade.Abstractions;
using NexusMods.Cascade.Abstractions.Diffs;
using NexusMods.Cascade.Implementation;
using NexusMods.Cascade.Implementation.Diffs;
using NexusMods.MnemonicDB.Abstractions.Cascade.Flows;
using NexusMods.MnemonicDB.Abstractions.Cascade.Stages;
using NexusMods.MnemonicDB.Abstractions.DatomIterators;
using NexusMods.MnemonicDB.Abstractions.Query.SliceDescriptors;

namespace NexusMods.MnemonicDB.Abstractions.Cascade;

public static class Query
{
    public static readonly Inlet<IDb> Db = new();
    
    public static readonly SliceFlow Slices = new(Db);

    public static IDiffFlow<Datom> All(this IAttribute attr)
    {
        return Slices.GetFlow(new AttributeSlice());
    }
}
