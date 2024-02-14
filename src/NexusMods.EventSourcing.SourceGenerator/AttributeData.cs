using System.Collections.Generic;

namespace NexusMods.EventSourcing.SourceGenerator;

public class AttributeData
{
    public string Name { get; set; } = "";
    public string AttributeType { get; set; } = "";
    public string Description { get; set; } = "";
    public string Namespace { get; set; } = "";
    public string Entity { get; set; } = "";
}

public class AttributeGroup
{
    public string Namespace { get; set; } = "";
    public string Entity { get; set; } = "";
    public IEnumerable<AttributeData> Attributes { get; set; } = new List<AttributeData>();
}
