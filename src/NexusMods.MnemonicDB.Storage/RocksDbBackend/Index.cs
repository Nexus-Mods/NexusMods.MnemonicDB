using NexusMods.MnemonicDB.Abstractions.DatomIterators;
using NexusMods.MnemonicDB.Storage.Abstractions;

namespace NexusMods.MnemonicDB.Storage.RocksDbBackend;

public class Index<TComparator>(AttributeRegistry registry, IndexStore store) :
    AIndex<TComparator, IndexStore>(registry, store), IRocksDbIndex
   where TComparator : IDatomComparator<AttributeRegistry>;
