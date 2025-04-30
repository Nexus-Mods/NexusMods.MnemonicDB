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
    public Flow<(EntityId Id, TValueType Value, EntityId TxId)> AttributeHistoryFlow { get; }

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

                var delta = datom.IsRetract ? -1 : 1;
                output.Add((datom.E, ReadValue(datom.ValueSpan, datom.Prefix.ValueTag, resolver)), delta);
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

                var delta = datom.IsRetract ? -1 : 1;
                output.Add((datom.E, ReadValue(datom.ValueSpan, datom.Prefix.ValueTag, resolver), EntityId.From(datom.T.Value)), delta);
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
    
    private void AttributeHistoryStepFn(IToDiffSpan<IDb> input, DiffList<(EntityId Id, TValueType Value, EntityId TxId)> output)
    {
        var span = input.ToDiffSpan();
        var update = Cascade.Query.ToDbUpdate(span);

        if (update.UpdateType == UpdateType.None)
            return;

        else if (update.UpdateType == UpdateType.Init)
        {
            var historyDb = update.Next!.Connection.History();
            var basisTxId = update.Next!.BasisTxId;

            foreach (var datom in historyDb.Datoms(this))
            {
                // We don't care about retractions here, be we are interested in the assertion history of the datom
                if (datom.T.Value > basisTxId.Value || datom.IsRetract)
                    continue;
                output.Add((datom.E, ReadValue(datom.ValueSpan, datom.Prefix.ValueTag, historyDb.Connection.AttributeResolver), EntityId.From(datom.T.Value)), 1);
            }
        }
        else if (update.UpdateType == UpdateType.NextTx)
        {
            var resolver = update.Next!.Connection.AttributeResolver;
            var attrId = update.Next.AttributeCache.GetAttributeId(Id);
            foreach (var datom in update.Next.RecentlyAdded)
            {
                // We don't care about rerractions here, be we are interested in the history of the datom
                if (datom.A != attrId || datom.IsRetract)
                    continue;
                
                output.Add((datom.E, ReadValue(datom.ValueSpan, datom.Prefix.ValueTag, resolver), EntityId.From(datom.T.Value)), 1);
            }
        }
        else
        {
            // We're still moving forward, so we only need to emit the datoms that are newer than the old basis
            if (update.Prev!.BasisTxId.Value < update.Next!.BasisTxId.Value)
            {
                var historyDb = update.Next.Connection.History();
                foreach (var datom in historyDb.Datoms(this))
                {
                    if (datom.T.Value <= update.Prev.BasisTxId.Value)
                        continue;
                    output.Add((datom.E, ReadValue(datom.ValueSpan, datom.Prefix.ValueTag, historyDb.Connection.AttributeResolver), EntityId.From(datom.T.Value)), 1);
                }
                
            }
            // We're going backwards, so we need to retract any datoms we previously emitted that are newer than the new TxId
            else
            {
                var historyDb = update.Prev.Connection.History();
                foreach (var datom in historyDb.Datoms(this))
                {
                    if (datom.T.Value > update.Next.BasisTxId.Value)
                        continue;
                    output.Add((datom.E, ReadValue(datom.ValueSpan, datom.Prefix.ValueTag, historyDb.Connection.AttributeResolver), EntityId.From(datom.T.Value)), -1);
                }
                
            }
        }
    }

    public Flow<(EntityId Id, TValueType Value, EntityId TxId, TOther Other)> AttributeHistoryFlowWithOther<TOther>(
        IReadableAttribute<TOther> otherAttr)
    {
        return new UnaryFlow<IDb, (EntityId Id, TValueType Value, EntityId TxId, TOther)>
        {
            DebugInfo = new()
            {
                Name = "MnemonicDB Attribute History",
                Expression = Id!.ToString()
            },
            Upstream = [Cascade.Query.Db],
            StepFn = AttributeHistoryStepFnWithOther,
        };

        void AttributeHistoryStepFnWithOther(IToDiffSpan<IDb> input,
            DiffList<(EntityId Id, TValueType Value, EntityId TxId, TOther other)> output)
        {
            var span = input.ToDiffSpan();
            var update = Cascade.Query.ToDbUpdate(span);

            if (update.UpdateType == UpdateType.None)
                return;


            if (update.UpdateType == UpdateType.Init)
            {
                var historyDb = update.Next!.Connection.History();
                var basisTxId = update.Next!.BasisTxId;

                var otherAttrId = historyDb.AttributeCache.GetAttributeId(otherAttr.Id);
                foreach (var datom in historyDb.Datoms(this))
                {
                    // We don't care about retractions here, be we are interested in the assertion history of the datom
                    if (datom.T.Value > basisTxId.Value || datom.IsRetract)
                        continue;

                    var otherDatoms = historyDb.Connection.AsOf(datom.T).Datoms(datom.E);

                    foreach (var otherDatom in otherDatoms)
                    {
                        if (otherDatom.A != otherAttrId)
                            continue;
                        var otherValue = otherAttr.ReadValue(otherDatom.ValueSpan, otherDatom.Prefix.ValueTag,
                            historyDb.Connection.AttributeResolver);
                        output.Add(
                            (datom.E,
                                ReadValue(datom.ValueSpan, datom.Prefix.ValueTag,
                                    historyDb.Connection.AttributeResolver), EntityId.From(datom.T.Value), otherValue),
                            1);
                    }

                }
            }
            else if (update.UpdateType == UpdateType.NextTx)
            {
                var resolver = update.Next!.Connection.AttributeResolver;
                var attrId = update.Next.AttributeCache.GetAttributeId(Id);
                var otherAttrId = update.Next.AttributeCache.GetAttributeId(otherAttr.Id);
                foreach (var datom in update.Next.RecentlyAdded)
                {
                    // We don't care about retractions here, be we are interested in the history of the datom
                    if (datom.A != attrId || datom.IsRetract)
                        continue;

                    var otherDatoms = update.Next.Connection.AsOf(datom.T).Datoms(datom.E);
                    foreach (var otherDatom in otherDatoms)
                    {
                        if (otherDatom.A != otherAttrId)
                            continue;
                        var otherValue = otherAttr.ReadValue(otherDatom.ValueSpan, otherDatom.Prefix.ValueTag,
                            update.Next.Connection.AttributeResolver);
                        output.Add(
                            (datom.E, ReadValue(datom.ValueSpan, datom.Prefix.ValueTag, resolver),
                                EntityId.From(datom.T.Value), otherValue), 1);
                    }
                }
            }
            else
            {
                // We're still moving forward, so we only need to emit the datoms that are newer than the old basis
                if (update.Prev!.BasisTxId.Value < update.Next!.BasisTxId.Value)
                {
                    var historyDb = update.Next.Connection.History();
                    var otherAttrId = historyDb.AttributeCache.GetAttributeId(otherAttr.Id);
                    foreach (var datom in historyDb.Datoms(this))
                    {
                        if (datom.T.Value <= update.Prev.BasisTxId.Value)
                            continue;

                        var otherDatoms = historyDb.Connection.AsOf(datom.T).Datoms(datom.E);

                        foreach (var otherDatom in otherDatoms)
                        {
                            if (otherDatom.A != otherAttrId)
                                continue;
                            var otherValue = otherAttr.ReadValue(otherDatom.ValueSpan, otherDatom.Prefix.ValueTag,
                                historyDb.Connection.AttributeResolver);
                            output.Add(
                                (datom.E,
                                    ReadValue(datom.ValueSpan, datom.Prefix.ValueTag,
                                        historyDb.Connection.AttributeResolver), EntityId.From(datom.T.Value),
                                    otherValue), 1);
                        }
                    }

                }
                // We're going backwards, so we need to retract any datoms we previously emitted that are newer than the new TxId
                else
                {
                    var historyDb = update.Prev.Connection.History();
                    var otherAttrId = historyDb.AttributeCache.GetAttributeId(otherAttr.Id);
                    foreach (var datom in historyDb.Datoms(this))
                    {
                        if (datom.T.Value > update.Next.BasisTxId.Value)
                            continue;

                        var otherDatoms = historyDb.Connection.AsOf(datom.T).Datoms(datom.E);
                        foreach (var otherDatom in otherDatoms)
                        {
                            if (otherDatom.A != otherAttrId)
                                continue;
                            var otherValue = otherAttr.ReadValue(otherDatom.ValueSpan, otherDatom.Prefix.ValueTag,
                                historyDb.Connection.AttributeResolver);
                            output.Add(
                                (datom.E,
                                    ReadValue(datom.ValueSpan, datom.Prefix.ValueTag,
                                        historyDb.Connection.AttributeResolver), EntityId.From(datom.T.Value),
                                    otherValue), -1);
                        }
                    }

                }
            }
        }
    }

    public Flow<KeyedValue<EntityId, TValueType>> FlowFor(Flow<IDb> dbFlow)
    {
        return new UnaryFlow<IDb, KeyedValue<EntityId, TValueType>>()
        {
            StepFn = AttributeStepFn,
            Upstream = [dbFlow],
            DebugInfo = new DebugInfo
            { 
                Name = "MnemonicDB Attr", 
                Expression = Id.ToString()
            }
        };
    }
}
