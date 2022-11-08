using IFY.Phorm.ExampleApp.Data;
using IFY.Phorm.SqlClient;
using Microsoft.Data.SqlClient;
using System.Data;

const string DB_CONN = @"Server=(localdb)\ProjectModels;Database=PhormTests;MultipleActiveResultSets=True";

// Create database objects
using (var db = new SqlConnection(DB_CONN))
{
    db.Open();
    runSQL(db, "DROP SCHEMA IF EXISTS [ExampleApp]");
    runSQL(db, "CREATE SCHEMA [ExampleApp]");

    runSQL(db, @"CREATE TABLE [ExampleApp].[User] (
    [Id] BIGINT NOT NULL PRIMARY KEY IDENTITY(1, 1),
    [Name] NVARCHAR(100) NOT NULL UNIQUE
)");

    runSQL(db, @"CREATE PROC [ExampleApp].[usp_CreateUser] (
    @Id BIGINT OUTPUT,
    @Name NVARCHAR(100)
) AS
    SET NOCOUNT ON
    INSERT INTO [ExampleApp].[User] ([Name])
        SELECT @Name
    SET @Id = SCOPE_IDENTITY()
RETURN 1");
}

// Run app logic
var phorm = new SqlPhormSession(DB_CONN);
var gateway = new DbGateway(phorm);

var user1 = new UserDto { Name = "Anne" };
if (!gateway.CreateUser(user1))
{
    throw new Exception("Create user failed");
}

var user2 = new UserDto { Name = "Bert" };
if (!gateway.CreateUser(user2))
{
    throw new Exception("Create user failed");
}

var user3 = new UserDto { Name = "Claire" };
if (!gateway.CreateUser(user3))
{
    throw new Exception("Create user failed");
}

void runSQL(IDbConnection db, string sql)
{
    using var cmd = db.CreateCommand();
    cmd.CommandType = CommandType.Text;
    cmd.CommandText = sql;
    _ = cmd.ExecuteNonQuery();
}