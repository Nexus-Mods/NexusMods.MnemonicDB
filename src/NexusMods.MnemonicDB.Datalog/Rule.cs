using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NexusMods.MnemonicDB.Datalog;

public class Rule
{
    public Rule(Fact head, Fact[] body)
    {
        Head = head;
        Body = body;
    }

    public Fact Head { get; set; }
    public Fact[] Body { get; set; }

    public IReadOnlyList<Fact> Apply(Engine engine)
    {
        var bindings = new List<Dictionary<LVar, object>>() {new()};

        foreach (var condition in Body)
        {
            var newBindings = new List<Dictionary<LVar, object>>();
            foreach (var fact in engine.Facts)
            {
                foreach (var binding in bindings)
                {
                    var stubstitudedCondition = condition.Substitute(binding);
                    if (fact.Matches(stubstitudedCondition))
                    {
                        var newBinding = binding.ToDictionary();
                        
                        
                        for (var i = 0; i < condition.Args.Length; i++)
                        {
                            var arg = condition.Args[i];
                            if (arg is LVar lvar)
                            {
                                newBinding[lvar] = fact.Args[i];
                            }
                        }
                        newBindings.Add(newBinding);
                    }
                }
            }
            bindings = newBindings;
        }
        
        var inferredFacts = new List<Fact>();
        foreach (var binding in bindings)
        {
            inferredFacts.Add(Head.Substitute(binding));
        }
        
        return inferredFacts;
    }
    
    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.Append(Head);
        sb.Append(" :- ");
        for (var i = 0; i < Body.Length; i++)
        {
            sb.Append(Body[i]);
            if (i < Body.Length - 1)
                sb.Append(", ");
        }

        return sb.ToString();
    }
}
