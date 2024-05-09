using System;

namespace NexusMods.MnemonicDB.Abstractions.Models;

/// <summary>
/// Define a model as being a model in the database, and provide the namespace
/// for all the attributes directly on the model.
/// </summary>
/// <param name="nsName"></param>
[AttributeUsage(AttributeTargets.Interface)]
public class ModelAttribute(string nsName) : Attribute
{

}
