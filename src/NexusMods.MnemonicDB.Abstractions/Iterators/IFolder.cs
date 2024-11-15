namespace NexusMods.MnemonicDB.Abstractions.Iterators;

public interface IFolder<TValue>
  where TValue : allows ref struct
{
    public void Start();
    public bool Add(in TValue val);
    public void Finish();
}
