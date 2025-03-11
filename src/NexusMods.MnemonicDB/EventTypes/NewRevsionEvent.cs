using System.Threading.Tasks;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.MnemonicDB.EventTypes;

/// <summary>
/// A new DB revision event
/// </summary>
internal record NewRevisionEvent(IDb? Prev, IDb Db, TaskCompletionSource OnFinished) : IEvent;
