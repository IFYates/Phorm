using IFY.Phorm.Execution;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IFY.Phorm.SqlClient.IntegrationTests;

[TestClass]
public class TransactionTests : SqlIntegrationTestBase
{
    [PhormContract(Name = "GetTestTable", Target = DbObjectType.Table)]
    public class DataItem : IUpsert
    {
        public long Id { get; set; }
        public string Text { get; set; }

        public DataItem(long id, string text)
        {
            Id = id;
            Text = text;
        }
        public DataItem() : this(default, string.Empty)
        { }
    }

    [PhormContract(Name = "GetTest_Upsert")]
    public interface IUpsert
    {
        long Id { set; }
        string Text { get; }
    }

    private void setupGetTestSchema(AbstractPhormSession phorm)
    {
        SqlTestHelpers.ApplySql(phorm, @"DROP TABLE IF EXISTS [dbo].[GetTestTable]");
        SqlTestHelpers.ApplySql(phorm, @"CREATE TABLE [dbo].[GetTestTable] (
	[Id] BIGINT NOT NULL IDENTITY(1,1) PRIMARY KEY,
	[Text] VARCHAR(256) NULL
)");

        SqlTestHelpers.ApplySql(phorm, @"CREATE OR ALTER PROC [dbo].[usp_GetTest_Upsert]
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
RETURN @@ROWCOUNT");
    }

    [TestMethod]
    public void Transaction()
    {
        var phorm = getPhormSession();
        setupGetTestSchema(phorm);

        // Act
        _ = phorm.Call("GetTest_Upsert", new { Text = "Aaa" });
        var data1 = ((IPhormSession)phorm).Get<DataItem[]>()!;

        DataItem[] data2;
        using (var transaction = phorm.BeginTransaction())
        {
            _ = transaction.Call("GetTest_Upsert", new { Text = "Bbb" });
            data2 = transaction.Get<DataItem[]>()!;
        }

        _ = phorm.Call("GetTest_Upsert", new { Text = "Ccc" });
        var data3 = ((IPhormSession)phorm).Get<DataItem[]>()!;

        // Assert
        Assert.AreEqual("Aaa", string.Join(',', data1.Select(d => d.Text)));
        Assert.AreEqual("Aaa,Bbb", string.Join(',', data2.Select(d => d.Text)));
        Assert.AreEqual("Aaa,Ccc", string.Join(',', data3.Select(d => d.Text)));
    }
}
