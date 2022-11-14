using Microsoft.Data.SqlClient;
using System.Data;

namespace IFY.Phorm.ExampleApp;

public static class DatabaseHelper
{
    public const string DB_CONN = @"Server=(localdb)\ProjectModels;Database=PhormTests;MultipleActiveResultSets=True";

    public static void ResetDatabase()
    {
        // Create database objects
        using var db = new SqlConnection(DB_CONN);

        db.Open();
        runSQL(db, "DROP PROC IF EXISTS [ExampleApp].[usp_CreateManager]");
        runSQL(db, "DROP PROC IF EXISTS [ExampleApp].[usp_CreateEmployee]");
        runSQL(db, "DROP PROC IF EXISTS [ExampleApp].[usp_GetAllPeople]");
        runSQL(db, "DROP PROC IF EXISTS [ExampleApp].[usp_GetManager]");
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

        // Fetch all people using gen-spec pattern
        runSQL(db, @"CREATE PROC [ExampleApp].[usp_GetAllPeople]
        AS
            SET NOCOUNT ON
            SELECT [Id], [Name], CONVERT(BIGINT, NULL) [ManagerId], [DepartmentName], 'Manager' [PersonType]
                FROM [ExampleApp].[Person] P JOIN [ExampleApp].[Manager] M ON M.[PersonId] = P.[Id]
            UNION ALL
            SELECT [Id], [Name], [ManagerId], NULL, 'Employee' [PersonType]
                FROM [ExampleApp].[Person] P JOIN [ExampleApp].[Employee] E ON E.[PersonId] = P.[Id]
        RETURN 1");

        // Fetch manager with all employees using child recordset
        runSQL(db, @"CREATE PROC [ExampleApp].[usp_GetManager] (
            @Id BIGINT
        ) AS
            SET NOCOUNT ON
            SELECT [Id], [Name], CONVERT(BIGINT, NULL) [ManagerId], [DepartmentName], 'Manager' [PersonType]
                FROM [ExampleApp].[Person] P JOIN [ExampleApp].[Manager] M ON M.[PersonId] = P.[Id]
                WHERE [Id] = @Id

            SELECT [Id], [Name], [ManagerId], NULL, 'Employee' [PersonType]
                FROM [ExampleApp].[Person] P JOIN [ExampleApp].[Employee] E ON E.[PersonId] = P.[Id]
                WHERE [ManagerId] = @Id
        RETURN 1");
    }

    private static void runSQL(IDbConnection db, string sql)
    {
        using var cmd = db.CreateCommand();
        cmd.CommandType = CommandType.Text;
        cmd.CommandText = sql;
        _ = cmd.ExecuteNonQuery();
    }
}
