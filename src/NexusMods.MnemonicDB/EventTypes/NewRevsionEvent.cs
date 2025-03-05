using System.Threading.Tasks;

namespace NexusMods.MnemonicDB.EventTypes;

/// <summary>
/// A new DB revision event
/// </summary>
internal record NewRevisionEvent(Db? Prev, Db Db, TaskCompletionSource OnFinished) : IEvent;
