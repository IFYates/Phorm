using IFY.Phorm.Execution;

namespace IFY.Phorm.SqlClient.IntegrationTests;

[TestClass]
public class TransactionTests : SqlIntegrationTestBase
{
    [PhormContract(Name = "GetTestTable", Target = DbObjectType.Table)]
    public class DataItem(long id, string text) : IUpsert
    {
        public long Id { get; set; } = id;
        public string Text { get; set; } = text;

        public DataItem() : this(default, string.Empty)
        { }
    }

    [PhormContract(Name = "GetTest_Upsert")]
    public interface IUpsert
    {
        long Id { set; }
        string Text { get; }
    }

    private async Task setupGetTestSchema(AbstractPhormSession phorm)
    {
        await SqlTestHelpers.ApplySql(phorm, TestContext.CancellationToken, [
            @"DROP TABLE IF EXISTS [dbo].[GetTestTable]",
            @"CREATE TABLE [dbo].[GetTestTable] (
	[Id] BIGINT NOT NULL IDENTITY(1,1) PRIMARY KEY,
	[Text] VARCHAR(256) NULL
)",
        @"CREATE OR ALTER PROC [dbo].[usp_GetTest_Upsert]
	@Id BIGINT = NULL OUTPUT,
	@Text VARCHAR(256) = NULL
AS
	SET NOCOUNT ON
	IF (@Id IS NULL) BEGIN
		INSERT [dbo].[GetTestTable] ([Text]) SELECT @Text
		SET @Id = SCOPE_IDENTITY()
		RETURN 1
	END

	UPDATE [dbo].[GetTestTable] SET [Text] = @Text WHERE [Id] = @Id
RETURN @@ROWCOUNT"
        ]);
    }

    [TestMethod]
    public async Task Transaction()
    {
        var phorm = getPhormSession();
        await setupGetTestSchema(phorm);

        // Act
        await phorm.CallAsync("GetTest_Upsert", new { Text = "Aaa" }, TestContext.CancellationToken);
        var data1 = await ((IPhormSession)phorm).GetAsync<DataItem[]>(TestContext.CancellationToken);

        DataItem[]? data2;
        using (var transaction = await phorm.BeginTransactionAsync(TestContext.CancellationToken))
        {
            await transaction.CallAsync("GetTest_Upsert", new { Text = "Bbb" }, TestContext.CancellationToken);
            data2 = await transaction.GetAsync<DataItem[]>(TestContext.CancellationToken);
        }

        await phorm.CallAsync("GetTest_Upsert", new { Text = "Ccc" }, TestContext.CancellationToken);
        var data3 = await ((IPhormSession)phorm).GetAsync<DataItem[]>(TestContext.CancellationToken);

        // Assert
        Assert.AreEqual("Aaa", string.Join(',', data1!.Select(d => d.Text)));
        Assert.AreEqual("Aaa,Bbb", string.Join(',', data2!.Select(d => d.Text)));
        Assert.AreEqual("Aaa,Ccc", string.Join(',', data3!.Select(d => d.Text)));
    }
}
