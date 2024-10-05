namespace NexusMods.MnemonicDB.Datalog;

public class LVar
{
    public LVar(string name)
    {
        Name = string.Intern(name);
    }

    public string Name { get; set; }

    public override string ToString()
    {
        return $"?{Name}";
    }

    public override bool Equals(object? obj)
    {
        if (obj is LVar lvar)
        {
            return ReferenceEquals(this, lvar);
        }
        return false;
    }
    
    public override int GetHashCode()
    {
        return Name.GetHashCode();
    }
}
