using NexusMods.MneumonicDB.Abstractions.DatomIterators;
using NexusMods.MneumonicDB.Storage.Abstractions;

namespace NexusMods.MneumonicDB.Storage.RocksDbBackend;

public class Index<TComparator>(AttributeRegistry registry, IndexStore store) :
    AIndex<TComparator, IndexStore>(registry, store), IRocksDbIndex
   where TComparator : IDatomComparator<AttributeRegistry>;
