using System.Collections.Generic;
using System.Linq;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.QueryEngine.Tables;

namespace NexusMods.MnemonicDB.QueryEngine.Predicates;

public record Datoms<TAttribute, TValue> : Predicate<EntityId, TAttribute, TValue>
where TAttribute : IWritableAttribute<TValue>, IReadableAttribute<TValue>
where TValue : notnull
{
    private static readonly Symbol LocalName = Symbol.Intern("datoms");
    public Datoms(Term<EntityId> eTerm, TAttribute attr, Term<TValue> vTerm) : base(LocalName, eTerm, attr, vTerm)
    {
    }

    protected override ITable Evaluate(IDb db, LVar<EntityId> item1LVar, TAttribute item2Value, LVar<TValue> item3LVar)
    {
        var appender = new AppendableTable([item1LVar, item3LVar]);
        var eRows = (IAppendableColumn<EntityId>)appender[0];
        var vRows = (IAppendableColumn<TValue>)appender[1];
        foreach (var datom in db.Datoms(item2Value))
        {
            eRows.Add(datom.E);
            var v = item2Value.ReadValue(datom.ValueSpan, datom.Prefix.ValueTag, db.Connection.AttributeResolver);
            vRows.Add(v);
            appender.FinishRow();
        }
        return appender.Freeze();
    }

    public override string ToString()
    {
        return base.ToString();
    }
}
