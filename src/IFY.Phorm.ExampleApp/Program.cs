using IFY.Phorm.ExampleApp.Data;
using IFY.Phorm.SqlClient;

namespace IFY.Phorm.ExampleApp;

public static class Program
{
    public static void Main()
    {
        DatabaseHelper.RunScript("ExampleApp_Remove.sql");
        DatabaseHelper.RunScript("ExampleApp.sql");

        // Setup
        var phorm = new SqlPhormSession(DatabaseHelper.DB_CONN);
        var gateway = new DbGateway(phorm);

        // Create staff
        Console.WriteLine("Creating staff:");
        var anne = new ManagerDto { Name = "Anne", Department = "Head Office" };
        if (!gateway.CreateManager(anne))
        {
            throw new Exception("Create manager failed");
        }
        Console.WriteLine($"- Created {anne.Name} ({anne.GetType().Name}): {anne.Id}");

        var bert = new EmployeeDto { Name = "Bert", Manager = anne };
        if (!gateway.CreateEmployee(bert))
        {
            throw new Exception("Create employee failed");
        }
        Console.WriteLine($"- Created {bert.Name} ({bert.GetType().Name}): {bert.Id}");

        var claire = new EmployeeDto { Name = "Claire", Manager = anne };
        if (!gateway.CreateEmployee(claire))
        {
            throw new Exception("Create employee failed");
        }
        Console.WriteLine($"- Created {claire.Name} ({claire.GetType().Name}): {claire.Id}");

        Console.WriteLine();

        // Get everyone
        Console.WriteLine("Get all staff:");
        var people = gateway.GetAllPeople();
        foreach (var person in people.All())
        {
            Console.WriteLine($"- Found {person.Name} ({person.GetType().Name}): {person.Id}");
        }
        Console.WriteLine();

        // Get a manager
        Console.WriteLine("Fetch a manager:");
        var manager = gateway.GetManager(anne.Id);
        Console.WriteLine($"- Manager {manager.Name} ({manager.Department}) has {manager.Employees.Length} employees:");
        foreach (var employee in manager.Employees)
        {
            Console.WriteLine($"    - {employee.Name} ({employee.Id})");
        }

        Console.WriteLine();
        Console.WriteLine("Finished.");
        Console.ReadLine();
    }
}