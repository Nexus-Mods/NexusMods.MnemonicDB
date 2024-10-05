using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.MnemonicDB.Datalog;

public class Engine
{
    private List<Fact> _facts = new();
    private List<Rule> _rules = new();
    
    public IEnumerable<Fact> Facts => _facts;
    
    public void Add(Fact fact)
    {
        _facts.Add(fact);
    }
    
    public void Add(Rule rule)
    {
        _rules.Add(rule);
    }
    
    public IEnumerable<Fact> Query(Fact query)
    {
        // Initial Facts
        var results = _facts.Where(fact => fact.Matches(query)).ToList();
        
        // Apply Rules
        foreach (var rule in _rules)
        {
            var inferredFacts = rule.Apply(this);
            foreach (var fact in inferredFacts)
            {
                if (fact.Matches(query))
                {
                    results.Add(fact);
                }
            }
        }
        return results;
    }

    public void Insert(Symbol employee, params object[] args)
    {
        Add(new Fact(employee, args));
    }
}
