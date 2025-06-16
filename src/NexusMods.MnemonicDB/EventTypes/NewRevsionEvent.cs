using System.Threading;
using System.Threading.Tasks;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.MnemonicDB.EventTypes;

/// <summary>
/// A new DB revision event
/// </summary>
internal record NewRevisionEvent(IDb? Prev, IDb Db, ManualResetEventSlim OnFinished) : IEvent;
