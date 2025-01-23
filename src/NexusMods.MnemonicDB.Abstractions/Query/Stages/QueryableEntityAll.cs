using NexusMods.Cascade;
using NexusMods.Cascade.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.MnemonicDB.Abstractions.Query.Stages;

public class QueryableEntityAll<T> : AUnaryStageDefinition<DbTransition, T> where T : IQueryableEntity<T>
{
    public QueryableEntityAll(UpstreamConnection upstream) : base(upstream)
    {
    }

    private sealed class Stage<T> : AUnaryStageDefinition<DbTransition, T>.Stage where T : notnull, IQueryableEntity<T>
    {
        private readonly QueryableEntityAll<T> _definition;

        public Stage(IFlowImpl flow, QueryableEntityAll<T> definition) : base(flow, definition)
        {
            _definition = definition;
        }

        protected override void Process(ChangeSet<DbTransition> input, ChangeSet<T> output)
        {
            foreach (var t in input)
            {
                // Drop retractions as we only ever care about the changes
                if (t.Delta < 0)
                    continue;

                switch (t.Value.Previous != null, t.Value.New != null)
                {
                    case (false, false):
                        // No change
                        continue;
                    case (false, true):
                        EmitFullDatoms(t.Value.New!, output);
                        continue;
                    case (true, false):
                        RetractAllDatoms(t.Value.Previous!, output);
                        continue;
                    case (true, true) when t.Value.Previous!.BasisTxId.Value + 1 == t.Value.New!.BasisTxId.Value && 
                            t.Value.Previous.Connection == t.Value.New.Connection:
                        // A common case where we can just move forward using the delta
                        MoveForward(t.Value.Previous!, t.Value.New!, output);
                        continue;
                }
            }
        }

        private void MoveForward(IDb valuePrevious, IDb valueNew, ChangeSet<T> output)
        {
            throw new System.NotImplementedException();
        }

        private void RetractAllDatoms(IDb valuePrevious, ChangeSet<T> output)
        {
            throw new System.NotImplementedException();
        }

        private void EmitFullDatoms(IDb valueNew, ChangeSet<T> output)
        {
            foreach (var d in valueNew.Datoms(T.PrimaryAttribute))
            {
                output.Add(T.Create(d.E), 1);
            }
        }
    }

    public override IStage CreateInstance(IFlowImpl flow)
    {
        return new Stage<T>(flow, this);
    }
}

