using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.MnemonicDB.LogicEngine;

public static class BuiltinGoals
{ 
    private static string Namespace => "NexusMods.MnemonicDB.LogicEngine.BuiltinGoals";
    public static readonly Symbol And = Symbol.Intern(Namespace, "And");
}
