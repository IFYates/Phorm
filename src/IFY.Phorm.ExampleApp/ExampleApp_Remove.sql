-- Clear out objects
DROP PROC IF EXISTS [ExampleApp].[usp_CreateManager]
GO
DROP PROC IF EXISTS [ExampleApp].[usp_CreateEmployee]
GO
DROP PROC IF EXISTS [ExampleApp].[usp_GetAllPeople]
GO
DROP PROC IF EXISTS [ExampleApp].[usp_GetManager]
GO
DROP TABLE IF EXISTS [ExampleApp].[Employee]
GO
DROP TABLE IF EXISTS [ExampleApp].[Manager]
GO
DROP TABLE IF EXISTS [ExampleApp].[Person]
GO
DROP SCHEMA IF EXISTS [ExampleApp]
GO