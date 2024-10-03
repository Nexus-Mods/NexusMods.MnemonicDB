using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.MnemonicDB.Query.Tests;

public class FactStorageTests
{
    private static Symbol Employee = Symbol.Intern("Tests/Employee");
    private static Symbol Department = Symbol.Intern("Tests/Department");
    private static Symbol Project = Symbol.Intern("Tests/Project");
    private static Symbol WorksOn = Symbol.Intern("Tests/WorksOn");
    private static Symbol Role = Symbol.Intern("Tests/Role");
    private static Symbol Manager = Symbol.Intern("Tests/Manager");
    
    private static LVar<string> LName = LVar<string>.Create("Name");
    private static LVar<int> LAge = LVar<int>.Create("Age");
    private static LVar<int> LEmployeeId = LVar<int>.Create("EmployeeId");
    
    private readonly FactStorage _factStorage;

    public FactStorageTests()
    {
        _factStorage = new FactStorage();
        
        // Employees
        // Name, Age, EmployeeId
        _factStorage.Insert(Employee, "Alice", 29, 101);
        _factStorage.Insert(Employee, "Bob", 35, 101);
        _factStorage.Insert(Employee, "Charlie", 40, 102);
        _factStorage.Insert(Employee, "Dana", 28, 103);
        _factStorage.Insert(Employee, "Eve", 31, 104);
        _factStorage.Insert(Employee, "Frank", 38, 102);

        // Departments
        _factStorage.Insert(Department, 101, "Engineering");
        _factStorage.Insert(Department, 102, "HR");
        _factStorage.Insert(Department, 103, "Marketing");
        _factStorage.Insert(Department, 104, "Sales");

        // Projects
        _factStorage.Insert(Project, 201, "AI Platform", 101);
        _factStorage.Insert(Project, 202, "HR Analytics", 102);
        _factStorage.Insert(Project, 203, "Brand Campaign", 103);
        _factStorage.Insert(Project, 204, "Sales Forecasting", 104);

        // Employees working on projects
        _factStorage.Insert(WorksOn, 1, 201); // Alice working on AI Platform
        _factStorage.Insert(WorksOn, 2, 201); // Bob working on AI Platform
        _factStorage.Insert(WorksOn, 3, 202); // Charlie working on HR Analytics
        _factStorage.Insert(WorksOn, 4, 203); // Dana working on Brand Campaign
        _factStorage.Insert(WorksOn, 5, 204); // Eve working on Sales Forecasting
        _factStorage.Insert(WorksOn, 6, 202); // Frank working on HR Analytics

        // Roles
        _factStorage.Insert(Role, 1, "Software Engineer");
        _factStorage.Insert(Role, 2, "DevOps Engineer");
        _factStorage.Insert(Role, 3, "HR Specialist");
        _factStorage.Insert(Role, 4, "Marketing Specialist");
        _factStorage.Insert(Role, 5, "Sales Manager");
        _factStorage.Insert(Role, 6, "HR Manager");

        // Management hierarchy
        _factStorage.Insert(Manager, 2, 1); // Bob reports to Alice
        _factStorage.Insert(Manager, 3, 2); // Charlie reports to Bob
        _factStorage.Insert(Manager, 5, 4); // Eve reports to Dana
    }
    
    
    [Fact]
    public async Task CanGetFacts()
    {
        var facts = _factStorage.Get(Goal.Create<string, int, int>(Employee, LName, LAge, LEmployeeId));
        await VerifyFacts(facts);
    }
    
    private static async Task VerifyFacts<TFact>(IEnumerable<TFact> facts)
    where TFact : IFact
    {
        var factStrings = facts.Select(f => f.ToString()).ToList();
        await Verify(factStrings);
    }
}
