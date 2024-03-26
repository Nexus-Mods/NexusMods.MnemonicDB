﻿using System.Runtime.InteropServices;
using NexusMods.EventSourcing.Abstractions;

namespace NexusMods.EventSourcing.Storage.Abstractions;

/// <summary>
/// Encodes and decodes the prefix of a key, the format is:
/// [AttributeId: 2bytes]
/// [TxId: 8bytes]
/// [EntityID + PartitionID: 7bytes]
/// [Flags: 1byte]
///
/// The Entity Id is created by taking the last 6 bytes of the id and combining it with
/// the partition id. So the encoding logic looks like this:
///
/// packed = (e & 0x00FFFFFFFFFFFFFF) >> 8 | (e & 0xFFFFFFFFFFFF) << 8
///
/// </summary>
[StructLayout(LayoutKind.Explicit, Size = Size)]
public struct KeyPrefix
{
    public const int Size = 16;

    [FieldOffset(0)]
    private ulong _upper;
    [FieldOffset(8)]
    private ulong _lower;

    public void Set(EntityId id, AttributeId attributeId, TxId txId, bool isRetract)
    {
        _upper = (ulong)attributeId << 48 | (ulong)txId & 0x0000FFFFFFFFFFFF;
        _lower = (ulong)id & 0xFF00000000000000 | ((ulong)id & 0x0000FFFFFFFFFFFF) << 8 | (isRetract ? 1UL : 0UL);
    }

    public EntityId E => (EntityId)((_lower & 0xFF00000000000000) | (_lower >> 8) & 0x0000FFFFFFFFFFFF);

    public AttributeId A => (AttributeId)(_upper >> 48);

    public TxId T => (TxId)Ids.MakeId(Ids.Partition.Tx, _upper & 0x0000FFFFFFFFFFFF);

    public bool IsRetract => (_lower & 1) == 1;

    public override string ToString() => $"E: {E}, A: {A}, T: {T}, Retract: {IsRetract}";
}