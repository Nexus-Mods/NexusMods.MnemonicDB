using NexusMods.MnemonicDB.Abstractions.DatomIterators;
using NexusMods.MnemonicDB.Storage.Abstractions;

namespace NexusMods.MnemonicDB.Storage.RocksDbBackend;

public class Index<TComparator>(IndexStore store) :
    AIndex<TComparator, IndexStore>(store), IRocksDbIndex
   where TComparator : IDatomComparator;
