using System;
using DynamicData;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.MnemonicDB.EventTypes;

/// <summary>
/// Request to unsubscribe the given observer from the database.
/// </summary>
public record UnSubscribeEvent(IObserver<ChangeSet<Datom, DatomKey, IDb>> Observer) : IEvent;
