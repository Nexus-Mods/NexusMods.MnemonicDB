using System.Collections.Generic;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.MnemonicDB.QueryFunctions;

public interface IRevisableFromAttributes
{
    void ReviseFromAttrs(IReadOnlySet<IAttribute> attrs);
}
