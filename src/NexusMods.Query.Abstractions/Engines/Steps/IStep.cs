namespace NexusMods.Query.Abstractions.Engines.Steps;

public interface IStep
{
    public void Execute(ref Environment environment);
}
