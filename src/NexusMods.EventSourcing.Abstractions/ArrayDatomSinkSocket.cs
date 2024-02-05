using System;

namespace NexusMods.EventSourcing.Abstractions;

public struct ArrayDatomSinkSocket((ulong E, ulong A, object V, ulong Tx)[] datoms) : IDatomSinkSocket
{
    public void Process<TSink>(ref TSink sink) where TSink : IDatomSink
    {
        foreach (var (e, a, v, t) in datoms)
        {
            switch (v)
            {
                case int intValue:
                    sink.Emit(e, a, intValue, t);
                    break;
                case long longValue:
                    sink.Emit(e, a, longValue, t);
                    break;
                case ulong ulongValue:
                    sink.Emit(e, a, ulongValue, t);
                    break;
                case string stringValue:
                    sink.Emit(e, a, stringValue, t);
                    break;
                case UInt128 uint128Value:
                    sink.Emit(e, a, uint128Value, t);
                    break;
                case Double doubleValue:
                    sink.Emit(e, a, doubleValue, t);
                    break;
                case float floatValue:
                    sink.Emit(e, a, floatValue, t);
                    break;
                case byte[] byteArrayValue:
                    sink.Emit(e, a, byteArrayValue, t);
                    break;
                default:
                    throw new Exception("Not supported type for datom value." + v.GetType());
            }

        }
    }
}
