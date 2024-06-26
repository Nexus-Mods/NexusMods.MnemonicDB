﻿using System;
using System.Collections.Generic;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.MnemonicDB;

/// <inheritdoc />
public class CommitResult(IDb db, IDictionary<EntityId, EntityId> remaps) : ICommitResult
{
    /// <inheritdoc />
    public EntityId this[EntityId id] =>
        remaps.TryGetValue(id, out var found) ? found : id;

    /// <inheritdoc />
    public TxId NewTx => db.BasisTxId;

    /// <inheritdoc />
    public IDb Db => db;
}
