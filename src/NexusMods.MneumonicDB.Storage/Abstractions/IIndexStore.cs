using NexusMods.MneumonicDB.Abstractions;
using NexusMods.MneumonicDB.Abstractions.DatomIterators;

namespace NexusMods.MneumonicDB.Storage.Abstractions;

public interface IIndexStore
{
    IndexType Type { get; }
}
