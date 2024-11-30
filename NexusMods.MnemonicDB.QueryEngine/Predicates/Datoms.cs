using System;
using System.Collections;
using System.Collections.Generic;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Query;
using NexusMods.MnemonicDB.QueryEngine.Facts;

namespace NexusMods.MnemonicDB.QueryEngine.Predicates;

public record Datoms<TAttribute, TValue> : Predicate<EntityId, TAttribute, TValue>
where TAttribute : IWritableAttribute<TValue>, IReadableAttribute<TValue>
where TValue : notnull
{
    private static readonly Symbol LocalName = Symbol.Intern("datoms");
    public Datoms(Term<EntityId> eTerm, TAttribute attr, Term<TValue> vTerm) : base(LocalName, eTerm, attr, vTerm)
    {
    }

    protected override ITable<TFact> Evaluate<TFact>(IDb db, LVar<EntityId> item1LVar, TAttribute item2Value, LVar<TValue> item3LVar)
    {
        var sliceDescriptor = SliceDescriptor.Create(item2Value, db.AttributeCache);
        return (ITable<TFact>)new ResultTable(db, sliceDescriptor, item1LVar, item2Value, item3LVar);
    }
    


    private class ResultTable : ITable<Fact<EntityId, TValue>>
    {
        private readonly IDb _db;
        private readonly SliceDescriptor _sliceDescriptor;
        private readonly LVar _elVar;
        private readonly TAttribute _attr;
        private readonly LVar _vlVar;

        public ResultTable(IDb db, SliceDescriptor sliceDescriptor, LVar elVar, TAttribute attr, LVar vlVar)
        {
            _db = db;
            _sliceDescriptor = sliceDescriptor;
            _elVar = elVar;
            _attr = attr;
            _vlVar = vlVar;
        }

        public LVar[] Columns => [_elVar, _vlVar];
        public Type FactType => typeof(Fact<EntityId, TValue>);
        
        public IEnumerator<Fact<EntityId, TValue>> GetEnumerator()
        {
            foreach (var datom in _db.Snapshot.RefDatoms(_sliceDescriptor))
            {
                var v = _attr.ReadValue(datom.ValueSpan, datom.Prefix.ValueTag, _db.Connection.AttributeResolver);
                yield return new Fact<EntityId, TValue>(datom.E, v);
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    public override Type FactType => typeof(Fact<EntityId, TValue>);
    
    public override string ToString()
    {
        return base.ToString();
    }
}
