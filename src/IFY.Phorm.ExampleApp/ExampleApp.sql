CREATE SCHEMA [ExampleApp]
GO

-- Generic information for a person
CREATE TABLE [ExampleApp].[Person] (
    [Id] BIGINT NOT NULL PRIMARY KEY IDENTITY(1, 1),
    [Name] NVARCHAR(100) NOT NULL UNIQUE
)
GO

-- Specialised table for managers
CREATE TABLE [ExampleApp].[Manager] (
    [PersonId] BIGINT NOT NULL PRIMARY KEY REFERENCES [ExampleApp].[Person]([Id]), -- One-to-One relationship
    [DepartmentName] NVARCHAR(100) NOT NULL UNIQUE
)
GO

-- Specialised table for people with a manager
CREATE TABLE [ExampleApp].[Employee] (
    [PersonId] BIGINT NOT NULL PRIMARY KEY REFERENCES [ExampleApp].[Person]([Id]), -- One-to-One relationship
    [ManagerId] BIGINT REFERENCES [ExampleApp].[Manager]([PersonId])
)
GO

CREATE PROC [ExampleApp].[usp_CreateEmployee] (
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
RETURN 1
GO

CREATE PROC [ExampleApp].[usp_CreateManager] (
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
RETURN 1
GO

-- Fetch all people using gen-spec pattern
CREATE PROC [ExampleApp].[usp_GetAllPeople]
AS
    SET NOCOUNT ON
    SELECT [Id], [Name], CONVERT(BIGINT, NULL) [ManagerId], [DepartmentName], 'Manager' [PersonType]
        FROM [ExampleApp].[Person] P JOIN [ExampleApp].[Manager] M ON M.[PersonId] = P.[Id]
    UNION ALL
    SELECT [Id], [Name], [ManagerId], NULL, 'Employee' [PersonType]
        FROM [ExampleApp].[Person] P JOIN [ExampleApp].[Employee] E ON E.[PersonId] = P.[Id]
RETURN 1
GO

-- Fetch manager with all employees using child recordset
CREATE PROC [ExampleApp].[usp_GetManager] (
    @Id BIGINT
) AS
    SET NOCOUNT ON
    SELECT [Id], [Name], CONVERT(BIGINT, NULL) [ManagerId], [DepartmentName], 'Manager' [PersonType]
        FROM [ExampleApp].[Person] P JOIN [ExampleApp].[Manager] M ON M.[PersonId] = P.[Id]
        WHERE [Id] = @Id

    SELECT [Id], [Name], [ManagerId], NULL, 'Employee' [PersonType]
        FROM [ExampleApp].[Person] P JOIN [ExampleApp].[Employee] E ON E.[PersonId] = P.[Id]
        WHERE [ManagerId] = @Id
RETURN 1
GO