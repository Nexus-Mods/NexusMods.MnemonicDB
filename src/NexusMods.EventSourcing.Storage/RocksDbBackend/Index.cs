using NexusMods.EventSourcing.Storage.Abstractions;

namespace NexusMods.EventSourcing.Storage.RocksDbBackend;

public class Index<TA, TB, TC, TD, TF>(AttributeRegistry registry, IndexStore store) :
    AIndex<TA, TB, TC, TD, TF, IndexStore>(registry, store), IRocksDbIndex
    where TA : IElementComparer
    where TB : IElementComparer
    where TC : IElementComparer
    where TD : IElementComparer
    where TF : IElementComparer
{

}
