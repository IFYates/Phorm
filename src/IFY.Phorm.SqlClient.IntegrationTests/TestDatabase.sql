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
	SET NOCOUNT ON
	TRUNCATE TABLE [dbo].[DataTable]
	RETURN 1
GO

CREATE OR ALTER PROC [dbo].[usp_GetAll]
AS
	SET NOCOUNT ON
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
	SET NOCOUNT ON
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

CREATE OR ALTER PROC [dbo].[usp_PrintTest]
	@Text VARCHAR(256) = NULL
AS
	SET NOCOUNT ON
	RAISERROR (@Text, 0, 1) WITH NOWAIT;
	RETURN 1
GO

CREATE OR ALTER PROC [dbo].[usp_ErrorTest]
	@Text VARCHAR(256) = NULL
AS
	SET NOCOUNT ON
	RAISERROR (@Text, 18, 1) WITH NOWAIT;
	RETURN 1
GO
