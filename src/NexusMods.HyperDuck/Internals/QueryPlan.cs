using System.Text.Json.Serialization;

namespace NexusMods.HyperDuck.Internals;

public class QueryPlanNode
{
    [JsonPropertyName("name")]
    public required string Name { get; set; }

    [JsonPropertyName("children")] 
    public QueryPlanNode[] Children { get; set; } = [];
    
    [JsonPropertyName("extra_info")]
    public ExtraInfo? ExtraInfo { get; set; }
}

public class ExtraInfo
{
    [JsonPropertyName("Function")]
    public string? Function { get; set; }

    [JsonPropertyName("Estimated Cardinality")]
    public string EstimatedCardinality { get; set; } = "";
}
    
