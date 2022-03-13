DROP TABLE IF EXISTS [dbo].[DataTable]
GO
CREATE TABLE [dbo].[DataTable] (
	[Id] BIGINT NOT NULL IDENTITY(1,1) PRIMARY KEY,
	[Int] INT NULL,
	[Text] VARCHAR(256) NULL,
	[Data] VARBINARY(MAX) NULL,
	[DateTime] DATETIME2 NULL,
	[IsInView] BIT NOT NULL DEFAULT (1)
)
GO

CREATE OR ALTER VIEW [dbo].[vw_Data] AS SELECT * FROM [dbo].[DataTable] WHERE [IsInView] = 1
GO

CREATE OR ALTER PROC [dbo].[usp_ClearTable]
AS
	TRUNCATE TABLE [dbo].[DataTable]
	RETURN 1
GO

CREATE OR ALTER PROC [dbo].[usp_GetAll]
AS
	SELECT * FROM [dbo].[DataTable]
	RETURN 1
GO

CREATE OR ALTER PROC [dbo].[usp_Upsert]
	@Id BIGINT = NULL OUTPUT,
	@Int INT = NULL,
	@Text VARCHAR(256) = NULL,
	@Data VARBINARY(MAX) = NULL,
	@DateTime DATETIME2 = NULL,
	@IsInView BIT = NULL
AS
BEGIN
	IF (@Id IS NULL) BEGIN
		INSERT [dbo].[DataTable] ([Int], [Text], [Data], [DateTime], [IsInView])
			SELECT @Int, @Text, @Data, @DateTime, ISNULL(@IsInView, 1)
		SET @Id = SCOPE_IDENTITY()
		RETURN 1
	END

	UPDATE [dbo].[DataTable] SET
		[Int] = @Int,
		[Text] = @Text,
		[Data] = @Data,
		[DateTime] = @DateTime,
		[IsInView] = ISNULL(@IsInView, [IsInView])
		WHERE [Id] = @Id
	RETURN @@ROWCOUNT
END
GO

CREATE OR ALTER PROC [dbo].[usp_GenSpecTest]
AS
	SELECT 1 [Id], 'Aaa' [Key], 1 [TypeId], 12.34 [Number], CONVERT(VARCHAR(50), NULL) [String]
	UNION ALL
	SELECT 2, 'Bbb', 2, NULL, 'Value'
RETURN 1
GO