using FASTER.core;
using NexusMods.EventSourcing.Abstractions;

namespace NexusMods.EventSourcing.FasterKV;

public class TransactionFunctions : IFunctions<SpanByteAndMemory, SpanByteAndMemory, IEvent, IEvent, Empty> {
    public bool SingleReader(ref SpanByteAndMemory key, ref IEvent input, ref SpanByteAndMemory value, ref IEvent dst,
        ref ReadInfo readInfo)
    {
        throw new System.NotImplementedException();
    }

    public bool ConcurrentReader(ref SpanByteAndMemory key, ref IEvent input, ref SpanByteAndMemory value, ref IEvent dst,
        ref ReadInfo readInfo)
    {
        throw new System.NotImplementedException();
    }

    public void ReadCompletionCallback(ref SpanByteAndMemory key, ref IEvent input, ref IEvent output, Empty ctx, Status status,
        RecordMetadata recordMetadata)
    {
        throw new System.NotImplementedException();
    }

    public bool SingleWriter(ref SpanByteAndMemory key, ref IEvent input, ref SpanByteAndMemory src, ref SpanByteAndMemory dst,
        ref IEvent output, ref UpsertInfo upsertInfo, WriteReason reason)
    {
        throw new System.NotImplementedException();
    }

    public void PostSingleWriter(ref SpanByteAndMemory key, ref IEvent input, ref SpanByteAndMemory src, ref SpanByteAndMemory dst,
        ref IEvent output, ref UpsertInfo upsertInfo, WriteReason reason)
    {
        throw new System.NotImplementedException();
    }

    public bool ConcurrentWriter(ref SpanByteAndMemory key, ref IEvent input, ref SpanByteAndMemory src, ref SpanByteAndMemory dst,
        ref IEvent output, ref UpsertInfo upsertInfo)
    {
        throw new System.NotImplementedException();
    }

    public bool NeedInitialUpdate(ref SpanByteAndMemory key, ref IEvent input, ref IEvent output, ref RMWInfo rmwInfo)
    {
        throw new System.NotImplementedException();
    }

    public bool InitialUpdater(ref SpanByteAndMemory key, ref IEvent input, ref SpanByteAndMemory value, ref IEvent output,
        ref RMWInfo rmwInfo)
    {
        throw new System.NotImplementedException();
    }

    public void PostInitialUpdater(ref SpanByteAndMemory key, ref IEvent input, ref SpanByteAndMemory value, ref IEvent output,
        ref RMWInfo rmwInfo)
    {
        throw new System.NotImplementedException();
    }

    public bool NeedCopyUpdate(ref SpanByteAndMemory key, ref IEvent input, ref SpanByteAndMemory oldValue, ref IEvent output,
        ref RMWInfo rmwInfo)
    {
        throw new System.NotImplementedException();
    }

    public bool CopyUpdater(ref SpanByteAndMemory key, ref IEvent input, ref SpanByteAndMemory oldValue,
        ref SpanByteAndMemory newValue, ref IEvent output, ref RMWInfo rmwInfo)
    {
        throw new System.NotImplementedException();
    }

    public void PostCopyUpdater(ref SpanByteAndMemory key, ref IEvent input, ref SpanByteAndMemory oldValue,
        ref SpanByteAndMemory newValue, ref IEvent output, ref RMWInfo rmwInfo)
    {
        throw new System.NotImplementedException();
    }

    public bool InPlaceUpdater(ref SpanByteAndMemory key, ref IEvent input, ref SpanByteAndMemory value, ref IEvent output,
        ref RMWInfo rmwInfo)
    {
        throw new System.NotImplementedException();
    }

    public void RMWCompletionCallback(ref SpanByteAndMemory key, ref IEvent input, ref IEvent output, Empty ctx, Status status,
        RecordMetadata recordMetadata)
    {
        throw new System.NotImplementedException();
    }

    public bool SingleDeleter(ref SpanByteAndMemory key, ref SpanByteAndMemory value, ref DeleteInfo deleteInfo)
    {
        throw new System.NotImplementedException();
    }

    public void PostSingleDeleter(ref SpanByteAndMemory key, ref DeleteInfo deleteInfo)
    {
        throw new System.NotImplementedException();
    }

    public bool ConcurrentDeleter(ref SpanByteAndMemory key, ref SpanByteAndMemory value, ref DeleteInfo deleteInfo)
    {
        throw new System.NotImplementedException();
    }

    public void DisposeSingleWriter(ref SpanByteAndMemory key, ref IEvent input, ref SpanByteAndMemory src,
        ref SpanByteAndMemory dst, ref IEvent output, ref UpsertInfo upsertInfo, WriteReason reason)
    {
        throw new System.NotImplementedException();
    }

    public void DisposeCopyUpdater(ref SpanByteAndMemory key, ref IEvent input, ref SpanByteAndMemory oldValue,
        ref SpanByteAndMemory newValue, ref IEvent output, ref RMWInfo rmwInfo)
    {
        throw new System.NotImplementedException();
    }

    public void DisposeInitialUpdater(ref SpanByteAndMemory key, ref IEvent input, ref SpanByteAndMemory value, ref IEvent output,
        ref RMWInfo rmwInfo)
    {
        throw new System.NotImplementedException();
    }

    public void DisposeSingleDeleter(ref SpanByteAndMemory key, ref SpanByteAndMemory value, ref DeleteInfo deleteInfo)
    {
        throw new System.NotImplementedException();
    }

    public void DisposeDeserializedFromDisk(ref SpanByteAndMemory key, ref SpanByteAndMemory value)
    {
        throw new System.NotImplementedException();
    }

    public void CheckpointCompletionCallback(int sessionID, string sessionName, CommitPoint commitPoint)
    {
        throw new System.NotImplementedException();
    }
}
