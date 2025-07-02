using System.Collections.Generic;

namespace NexusMods.MnemonicDB.Abstractions;

public interface IQueryEngine
{
    IConnection DefaultConnection();
    IEnumerable<dynamic> Query(string select);
    
    IEnumerable<T> Query<T>(string select);
}
