namespace NexusMods.MnemonicDB.SourceGenerator;

public record MethodChain()
{
    public string Namespace { get; set; } = "";
    public MethodCall[] Methods { get; set; } = [];

    public ConcreteModel Analyze()
    {
        var model = new ConcreteModel();

        foreach (var method in Methods)
        {
            switch (method.MethodName)
            {
                case "New":
                    model.Name = method.Arguments[0].Value.ToString();
                    model.FullName = $"{Namespace}.{model.Name}";
                    model.Namespace = Namespace;
                    break;
                case "WithAttribute":
                {
                    var attribute = new ConcreteAttribute
                    {
                        Name = method.Arguments[0].Value.ToString(),
                        Type = method.GenericTypes![0]
                    };
                    model.Attributes.Add(attribute);
                    break;
                }
            }
        }
        return model;
    }
}
