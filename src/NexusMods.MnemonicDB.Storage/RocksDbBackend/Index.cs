using NexusMods.MnemonicDB.Abstractions.DatomComparators;
using NexusMods.MnemonicDB.Storage.Abstractions;

namespace NexusMods.MnemonicDB.Storage.RocksDbBackend;

public class Index<TComparator>(IndexStore<TComparator> store) :
    AIndex<TComparator, IndexStore<TComparator>>(store), IRocksDbIndex
   where TComparator : IDatomComparator;
