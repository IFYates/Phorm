using IFY.Phorm.ExampleApp.Data;
using IFY.Phorm.SqlClient;
using Microsoft.Data.SqlClient;
using System.Data;

const string DB_CONN = @"Server=(localdb)\ProjectModels;Database=PhormTests;MultipleActiveResultSets=True";

// Create database objects
using (var db = new SqlConnection(DB_CONN))
{
    db.Open();
    runSQL(db, "DROP PROC IF EXISTS [ExampleApp].[usp_CreateManager]");
    runSQL(db, "DROP PROC IF EXISTS [ExampleApp].[usp_CreateEmployee]");
    runSQL(db, "DROP TABLE IF EXISTS [ExampleApp].[Employee]");
    runSQL(db, "DROP TABLE IF EXISTS [ExampleApp].[Manager]");
    runSQL(db, "DROP TABLE IF EXISTS [ExampleApp].[Person]");
    runSQL(db, "DROP SCHEMA IF EXISTS [ExampleApp]");

    runSQL(db, "CREATE SCHEMA [ExampleApp]");

    // Generic information for a person
    runSQL(db, @"CREATE TABLE [ExampleApp].[Person] (
        [Id] BIGINT NOT NULL PRIMARY KEY IDENTITY(1, 1),
        [Name] NVARCHAR(100) NOT NULL UNIQUE
    )");

    // Specialised table for managers
    runSQL(db, @"CREATE TABLE [ExampleApp].[Manager] (
        [PersonId] BIGINT NOT NULL PRIMARY KEY REFERENCES [ExampleApp].[Person]([Id]), -- One-to-One relationship
        [DepartmentName] NVARCHAR(100) NOT NULL UNIQUE
    )");

    // Specialised table for people with a manager
    runSQL(db, @"CREATE TABLE [ExampleApp].[Employee] (
        [PersonId] BIGINT NOT NULL PRIMARY KEY REFERENCES [ExampleApp].[Person]([Id]), -- One-to-One relationship
        [ManagerId] BIGINT REFERENCES [ExampleApp].[Manager]([PersonId])
    )");

    runSQL(db, @"CREATE PROC [ExampleApp].[usp_CreateEmployee] (
        @Name NVARCHAR(100),
        @ManagerId BIGINT,
        @Id BIGINT OUTPUT
    ) AS
        SET NOCOUNT ON
        INSERT INTO [ExampleApp].[Person] ([Name])
            SELECT @Name
        SET @Id = SCOPE_IDENTITY()
        INSERT INTO [ExampleApp].[Employee] ([PersonId], [ManagerId])
            SELECT @Id, @ManagerId
    RETURN 1");

    runSQL(db, @"CREATE PROC [ExampleApp].[usp_CreateManager] (
        @Name NVARCHAR(100),
        @DepartmentName NVARCHAR(100),
        @Id BIGINT OUTPUT
    ) AS
        SET NOCOUNT ON
        INSERT INTO [ExampleApp].[Person] ([Name])
            SELECT @Name
        SET @Id = SCOPE_IDENTITY()
        INSERT INTO [ExampleApp].[Manager] ([PersonId], [DepartmentName])
            SELECT @Id, @DepartmentName
    RETURN 1");
}

// Run app logic
var phorm = new SqlPhormSession(DB_CONN);
var gateway = new DbGateway(phorm);

var anne = new ManagerDto { Name = "Anne" };
if (!gateway.CreateManager(anne))
{
    throw new Exception("Create manager failed");
}

var bert = new EmployeeDto { Name = "Bert", Manager = anne };
if (!gateway.CreateEmployee(bert))
{
    throw new Exception("Create employee failed");
}

var claire = new EmployeeDto { Name = "Claire", Manager = anne };
if (!gateway.CreateEmployee(claire))
{
    throw new Exception("Create employee failed");
}

void runSQL(IDbConnection db, string sql)
{
    using var cmd = db.CreateCommand();
    cmd.CommandType = CommandType.Text;
    cmd.CommandText = sql;
    _ = cmd.ExecuteNonQuery();
}