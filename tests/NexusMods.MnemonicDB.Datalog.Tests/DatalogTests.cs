using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.MnemonicDB.Datalog;

public class DatalogTests
{
// Symbol for predicates
    private static readonly Symbol Manager = Symbol.Intern("Tests/Manager");
    private static readonly Symbol Employee = Symbol.Intern("Tests/Employee");
    private static readonly Symbol Department = Symbol.Intern("Tests/Department");
    private static readonly Symbol WorksUnder = Symbol.Intern("Tests/WorksUnder");
    private static readonly Symbol SameDepartment = Symbol.Intern("Tests/SameDepartment");
    private static readonly Symbol SharesManager = Symbol.Intern("Tests/SharesManager");

    private readonly Engine _datalog;

    public DatalogTests()
    {
        _datalog = new Engine();

        // Insert test data
        InsertTestData();

        // Insert rules
        InsertRules();
    }

    private void InsertTestData()
    {
        // Example test data for Managers and Employees in various Departments
        _datalog.Insert(Manager, "Alice", "Sales");
        _datalog.Insert(Employee, "Bob", "Sales");
        _datalog.Insert(Employee, "Charlie", "Sales");
        _datalog.Insert(Employee, "David", "Marketing");
        _datalog.Insert(Manager, "Eve", "Marketing");
        _datalog.Insert(Employee, "Frank", "Marketing");
        _datalog.Insert(Employee, "Grace", "IT");
        _datalog.Insert(Manager, "Heidi", "IT");
        _datalog.Insert(Employee, "Ivan", "IT");
        _datalog.Insert(Employee, "Judy", "HR");
        _datalog.Insert(Manager, "Mallory", "HR");
        _datalog.Insert(Employee, "Oscar", "HR");
    }

    private void InsertRules()
    {
        var e = new LVar("e");
        var e2 = new LVar("e2");
        var m = new LVar("m");
        var d = new LVar("d");
        
        // Rule: If someone is an Employee in a Department and there is a Manager of that Department,
        // then the Employee works under that Manager.
        _datalog.Add(new Rule(new Fact(WorksUnder, e, m),
            [new Fact(Employee, e, d),
            new Fact(Manager, m, d)]
        ));

        // Rule: If two employees work in the same department, they share that department.
        _datalog.Add(new Rule(new Fact(SameDepartment, e, m),
            [new Fact(Employee, e, d),
            new Fact(Employee, e2, d)]
        ));
        
        // Rule: If two employees work in the same department and work under the same manager,
        // they share a manager.
        _datalog.Add(new Rule(new Fact(SharesManager, e, e2),
            [new Fact(SameDepartment, e, e2),
            new Fact(WorksUnder, e, m),
            new Fact(WorksUnder, e2, m)]
        ));
    }


    [Fact]
    public void CanQueryRelations()
    {
        var e = new LVar("e");
        var results = _datalog.Query(new Fact(WorksUnder, e, "Alice"));

        return;

    }
}
