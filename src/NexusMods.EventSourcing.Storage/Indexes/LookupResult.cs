namespace NexusMods.EventSourcing.Storage.Indexes;

public enum LookupResult : int
{
    NotFound = 0,
    Found = 1,
    FoundNewer = 2
}
