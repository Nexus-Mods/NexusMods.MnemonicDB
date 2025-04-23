// ReSharper disable CheckNamespace

using System;
using NexusMods.Cascade;
using NexusMods.Cascade.Abstractions;
using NexusMods.Cascade.Collections;
using NexusMods.Cascade.Flows;
using NexusMods.Cascade.Structures;
using NexusMods.MnemonicDB.Abstractions.Cascade;
using NexusMods.MnemonicDB.Abstractions.Cascade.Flows;

namespace NexusMods.MnemonicDB.Abstractions;

/// <summary>
///     Interface for a specific attribute
/// </summary>
public abstract partial class Attribute<TValueType, TLowLevelType, TSerializer> : UnaryFlow<IDb, KeyedValue<EntityId, TValueType>> 
    where TValueType : notnull
    where TSerializer : IValueSerializer<TLowLevelType>
{
    public Flow<(EntityId Id, TValueType Value, EntityId TxId)> AttributeWithTxIdFlow { get; }

    private void AttributeStepFn(IToDiffSpan<IDb> input, DiffList<KeyedValue<EntityId, TValueType>> output)
    {
        var span = input.ToDiffSpan();
        var update = Cascade.Query.ToDbUpdate(span);

        if (update.UpdateType == UpdateType.None)
            return;

        else if (update.UpdateType == UpdateType.Init)
        {
            var datoms = update.Next!.Datoms(this);
            var resolver = update.Next.Connection.AttributeResolver;
            foreach (var datom in datoms)
                output.Add((datom.E, ReadValue(datom.ValueSpan, datom.Prefix.ValueTag, resolver)), 1);
        }
        else if (update.UpdateType == UpdateType.NextTx)
        {
            var resolver = update.Next!.Connection.AttributeResolver;
            var attrId = update.Next.AttributeCache.GetAttributeId(Id);
            foreach (var datom in update.Next.RecentlyAdded)
            {
                if (datom.A != attrId)
                    continue;

                output.Add((datom.E, ReadValue(datom.ValueSpan, datom.Prefix.ValueTag, resolver)), 1);
            }
        }
        else
        {
            var resolver = update.Next!.Connection.AttributeResolver;

            var oldDatoms = update.Prev!.Datoms(this);
            foreach (var datom in oldDatoms)
                output.Add((datom.E, ReadValue(datom.ValueSpan, datom.Prefix.ValueTag, resolver)), -1);

            var newDatoms = update.Prev.Datoms(this);
            foreach (var datom in newDatoms)
                output.Add((datom.E, ReadValue(datom.ValueSpan, datom.Prefix.ValueTag, resolver)), 1);
        }
    }

    private void AttributeWithTxIdStepFn(IToDiffSpan<IDb> input, DiffList<(EntityId Id, TValueType Value, EntityId TxId)> output)
    {
        var span = input.ToDiffSpan();
        var update = Cascade.Query.ToDbUpdate(span);

        if (update.UpdateType == UpdateType.None)
            return;

        else if (update.UpdateType == UpdateType.Init)
        {
            var datoms = update.Next!.Datoms(this);
            var resolver = update.Next.Connection.AttributeResolver;
            foreach (var datom in datoms)
                output.Add((datom.E, ReadValue(datom.ValueSpan, datom.Prefix.ValueTag, resolver), EntityId.From(datom.T.Value)), 1);
        }
        else if (update.UpdateType == UpdateType.NextTx)
        {
            var resolver = update.Next!.Connection.AttributeResolver;
            var attrId = update.Next.AttributeCache.GetAttributeId(Id);
            foreach (var datom in update.Next.RecentlyAdded)
            {
                if (datom.A != attrId)
                    continue;

                output.Add((datom.E, ReadValue(datom.ValueSpan, datom.Prefix.ValueTag, resolver), EntityId.From(datom.T.Value)), 1);
            }
        }
        else
        {
            var resolver = update.Next!.Connection.AttributeResolver;

            var oldDatoms = update.Prev!.Datoms(this);
            foreach (var datom in oldDatoms)
                output.Add((datom.E, ReadValue(datom.ValueSpan, datom.Prefix.ValueTag, resolver), EntityId.From(datom.T.Value)), -1);

            var newDatoms = update.Prev.Datoms(this);
            foreach (var datom in newDatoms)
                output.Add((datom.E, ReadValue(datom.ValueSpan, datom.Prefix.ValueTag, resolver), EntityId.From(datom.T.Value)), 1);
        }
    }
    
}
