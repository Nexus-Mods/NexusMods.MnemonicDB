using NexusMods.EventSourcing.Storage.Abstractions;

namespace NexusMods.EventSourcing.Storage.RocksDbBackend;

public class Index<TA, TB, TC, TD, TF>(AttributeRegistry registry, IndexStore store, bool keepHistory) :
    AIndex<TA, TB, TC, TD, TF, IndexStore>(registry, store, keepHistory), IRocksDbIndex
    where TA : IElementComparer
    where TB : IElementComparer
    where TC : IElementComparer
    where TD : IElementComparer
    where TF : IElementComparer
{

}
