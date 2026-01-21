using IFY.Phorm.Data;
using IFY.Phorm.Execution;
using System.Collections;
using System.Reflection;
using System.Runtime.Serialization;

namespace IFY.Phorm.SqlClient.IntegrationTests;

[TestClass]
public class GetTests : SqlIntegrationTestBase
{
    static bool hasUnresolvedEntities(IEnumerable l)
    {
        var resolversField = l.GetType()
            .GetField("_resolvers", BindingFlags.Instance | BindingFlags.NonPublic)!;
        return ((ICollection)resolversField.GetValue(l)!).Count > 0;
    }

    [PhormContract(Name = "GetTestTable", Target = DbObjectType.Table)]
    public class DataItem(long id, int? num, string? text, byte[]? data, DateTime? dateTime) : IUpsert, IUpsertOnlyIntWithId, IUpsertWithId
    {
        public long Id { get; set; } = id;
        [DataMember(Name = "Int")]
        public int? Num { get; set; } = num;
        public string? Text { get; set; } = text;
        public byte[]? Data { get; set; } = data;
        public DateTime? DateTime { get; set; } = dateTime;
        public bool Flag { get; set; } = true;

        public DataItem() : this(default, default, default, default, default)
        { }
    }

    [PhormContract(Name = "GetTestTable", Target = DbObjectType.Table)]
    public class DataItemWithoutText(long id, int? num, string? text, byte[]? data, DateTime? dateTime)
    {
        public long Id { get; set; } = id;
        [DataMember(Name = "Int")]
        public int? Num { get; set; } = num;
        [IgnoreDataMember]
        public string? Text { get; set; } = text;
        public byte[]? Data { get; set; } = data;
        public DateTime? DateTime { get; set; } = dateTime;

        public DataItemWithoutText()
            : this(default, default, default, default, default)
        { }
    }

    [PhormContract(Name = "GetTest_Upsert")]
    public interface IUpsert : IPhormContract
    {
        [DataMember(Name = "Int")]
        int? Num { get; }
        string? Text { get; }
        byte[]? Data { get; }
        DateTime? DateTime { get; }
    }
    [PhormContract(Name = "GetTest_Upsert")]
    public interface IUpsertWithId : IUpsert
    {
        long Id { set; }
    }

    [PhormContract]
    public interface IGetAll : IPhormContract
    {
        //long Id { get; }
        //int? Num { get; }
        //string? Text { get; }
        //byte[]? Data { get; }
        //DateTime? DateTime { get; }
    }

    [PhormContract(Name = "GetTest_Upsert")]
    public interface IUpsertOnlyIntWithId : IPhormContract
    {
        long Id { set; }
        [DataMember(Name = "Int")]
        int? Num { get; }
    }

    [PhormContract(Name = "Data", Target = DbObjectType.View)]
    public interface IDataView : IPhormContract
    {
        long? Id { get; }
    }

    private async Task setupGetTestSchema(AbstractPhormSession phorm)
    {
        await SqlTestHelpers.ApplySql(phorm, TestContext.CancellationToken, [
            @"DROP TABLE IF EXISTS [dbo].[GetTestTable]",
            @"CREATE TABLE [dbo].[GetTestTable] (
	[Id] BIGINT NOT NULL IDENTITY(1,1) PRIMARY KEY,
	[Int] INT NULL,
	[Text] VARCHAR(256) NULL,
	[Data] VARBINARY(MAX) NULL,
	[DateTime] DATETIME2 NULL,
	[IsInView] BIT NOT NULL DEFAULT (1)
)",
            @"CREATE OR ALTER VIEW [dbo].[vw_Data] AS SELECT * FROM [dbo].[GetTestTable] WHERE [IsInView] = 1",
            @"CREATE OR ALTER PROC [dbo].[usp_GetAll]
AS
	SET NOCOUNT ON
	SELECT * FROM [dbo].[GetTestTable]
RETURN 1",
            @"CREATE OR ALTER PROC [dbo].[usp_GetTest_Upsert]
	@Id BIGINT = NULL OUTPUT,
	@Int INT = NULL,
	@Text VARCHAR(256) = NULL,
	@Data VARBINARY(MAX) = NULL,
	@DateTime DATETIME2 = NULL,
	@IsInView BIT = NULL
AS
	SET NOCOUNT ON
	IF (@Id IS NULL) BEGIN
		INSERT [dbo].[GetTestTable] ([Int], [Text], [Data], [DateTime], [IsInView])
			SELECT @Int, @Text, @Data, @DateTime, ISNULL(@IsInView, 1)
		SET @Id = SCOPE_IDENTITY()
		RETURN 1
	END

	UPDATE [dbo].[GetTestTable] SET
		[Int] = @Int,
		[Text] = @Text,
		[Data] = @Data,
		[DateTime] = @DateTime,
		[IsInView] = ISNULL(@IsInView, [IsInView])
		WHERE [Id] = @Id
RETURN @@ROWCOUNT"
        ]);
    }

    #region Many

    [TestMethod]
    public async Task Many__Can_access_returnvalue_of_sproc()
    {
        var phorm = getPhormSession();
        await setupGetTestSchema(phorm);

        await ((IPhormSession)phorm).CallAsync("GetTest_Upsert", TestContext.CancellationToken);
        await ((IPhormSession)phorm).CallAsync("GetTest_Upsert", TestContext.CancellationToken);
        await ((IPhormSession)phorm).CallAsync("GetTest_Upsert", TestContext.CancellationToken);
        await ((IPhormSession)phorm).CallAsync("GetTest_Upsert", TestContext.CancellationToken);

        var obj = new { ReturnValue = ContractMember.RetVal() };
        var res = await phorm.From<IGetAll>(obj)
            .GetAsync<DataItem[]>(TestContext.CancellationToken);

        Assert.AreEqual(1, obj.ReturnValue.Value);
        Assert.HasCount(4, res!);
    }

    [TestMethod]
    public async Task Many__All_from_view()
    {
        // Arrange
        var phorm = getPhormSession();
        await setupGetTestSchema(phorm);

        await phorm.CallAsync("GetTest_Upsert", new { Int = 0, IsInView = false }, TestContext.CancellationToken);
        await phorm.CallAsync("GetTest_Upsert", new { Int = 0, IsInView = false }, TestContext.CancellationToken);
        await phorm.CallAsync("GetTest_Upsert", new { Int = 1, IsInView = true }, TestContext.CancellationToken);
        await phorm.CallAsync("GetTest_Upsert", new { Int = 1, IsInView = true }, TestContext.CancellationToken);
        await phorm.CallAsync("GetTest_Upsert", new { Int = 1, IsInView = true }, TestContext.CancellationToken);

        // Act
        var res = await phorm.From<IDataView>(null)
            .GetAsync<DataItem[]>(TestContext.CancellationToken);

        // Assert
        Assert.HasCount(3, res!);
        Assert.IsTrue(res!.All(e => e.Num == 1));
    }

    [TestMethod]
    public async Task Many__Filtered_from_view()
    {
        // Arrange
        var phorm = getPhormSession();
        await setupGetTestSchema(phorm);

        var obj1 = new DataItem();
        var res1 = await phorm.CallAsync<IUpsertWithId>(obj1, TestContext.CancellationToken);

        var obj2 = new DataItem();
        var res2 = await phorm.CallAsync<IUpsertWithId>(obj2, TestContext.CancellationToken);

        // Act
        var res3 = await phorm.From<IDataView>(new { obj2.Id })
            .GetAsync<DataItem[]>(TestContext.CancellationToken);

        // Assert
        Assert.AreEqual(1, res1);
        Assert.AreEqual(1, res2);
        Assert.AreNotEqual(obj1.Id, obj2.Id);
        Assert.AreEqual(obj2.Id, res3!.Single().Id);
    }

    [TestMethod]
    public async Task Many__All_from_table()
    {
        var phorm = getPhormSession();
        await setupGetTestSchema(phorm);

        await phorm.CallAsync("GetTest_Upsert", null, TestContext.CancellationToken);
        await phorm.CallAsync("GetTest_Upsert", null, TestContext.CancellationToken);
        await phorm.CallAsync("GetTest_Upsert", null, TestContext.CancellationToken);

        var res = await phorm.GetAsync<DataItemWithoutText[]>(null!, TestContext.CancellationToken);

        Assert.HasCount(3, res!);
    }

    [TestMethod]
    public async Task Many__Filtered_from_table()
    {
        var phorm = getPhormSession();
        await setupGetTestSchema(phorm);

        await phorm.CallAsync("GetTest_Upsert", null, TestContext.CancellationToken);
        await phorm.CallAsync("GetTest_Upsert", null, TestContext.CancellationToken);
        await phorm.CallAsync("GetTest_Upsert", null, TestContext.CancellationToken);

        var res = await phorm.GetAsync<DataItem[]>(new { Id = 1 }, TestContext.CancellationToken);

        Assert.AreEqual(1, res!.Single().Id);
    }

    [TestMethod]
    public async Task Many__IEnumerable_resolves_later()
    {
        var phorm = getPhormSession();
        await setupGetTestSchema(phorm);

        await phorm.CallAsync("GetTest_Upsert", null, TestContext.CancellationToken);
        await phorm.CallAsync("GetTest_Upsert", null, TestContext.CancellationToken);
        await phorm.CallAsync("GetTest_Upsert", null, TestContext.CancellationToken);

        var res = await phorm.From<IDataView>(null).GetAsync<IEnumerable<DataItem>>(TestContext.CancellationToken);

        Assert.IsTrue(hasUnresolvedEntities(res!));
        Assert.HasCount(3, res!);
        Assert.IsTrue(hasUnresolvedEntities(res!));
        Assert.HasCount(3, res!.ToArray()); // Resolve the entities
        Assert.IsFalse(hasUnresolvedEntities(res!));
    }

    [TestMethod]
    public async Task Many__ICollection_resolves_later()
    {
        var phorm = getPhormSession();
        await setupGetTestSchema(phorm);

        await phorm.CallAsync("GetTest_Upsert", null, TestContext.CancellationToken);
        await phorm.CallAsync("GetTest_Upsert", null, TestContext.CancellationToken);
        await phorm.CallAsync("GetTest_Upsert", null, TestContext.CancellationToken);

        var res = await phorm.From<IDataView>(null).GetAsync<ICollection<DataItem>>(TestContext.CancellationToken);

        Assert.IsTrue(hasUnresolvedEntities(res!));
        Assert.HasCount(3, res!);
        Assert.IsTrue(hasUnresolvedEntities(res!));
        Assert.HasCount(3, res!.ToArray()); // Resolve the entities
        Assert.IsFalse(hasUnresolvedEntities(res!));
    }

    #endregion Many

    #region One

    [TestMethod]
    public async Task One__Can_ignore_property()
    {
        var phorm = getPhormSession();
        await setupGetTestSchema(phorm);

        var res = await phorm.CallAsync("GetTest_Upsert", null, TestContext.CancellationToken);

        var obj = await phorm.From<IDataView>(new { Id = 1 })
            .GetAsync<DataItemWithoutText>(TestContext.CancellationToken);

        Assert.AreEqual(1, res);
        Assert.IsNull(obj!.Text);
    }

    #endregion One

    #region Data

    enum MyEnum
    {
        None = 0,
        A,
        B,
        C,
        D
    }

    [PhormContract(Name = "GetTestTable", Target = DbObjectType.Table)]
    class EnumDto
    {
        public MyEnum A { get; set; }
        public MyEnum B { get; set; }
        public MyEnum C { get; set; }
        public MyEnum D { get; set; }
    }

    [TestMethod]
    public async Task Data__Supports_all_numeric_types_for_enum()
    {
        var phorm = getPhormSession();

        await SqlTestHelpers.ApplySql(phorm, TestContext.CancellationToken, [
            @"DROP TABLE IF EXISTS [dbo].[GetTestTable]",
            @"CREATE TABLE [dbo].[GetTestTable] (
	[A] TINYINT,
	[B] SMALLINT,
	[C] INT,
	[D] BIGINT
)",
            @"INSERT INTO [dbo].[GetTestTable] VALUES (1, 2, 3, 4)"
        ]);

        var dto = await ((IPhormSession)phorm).GetAsync<EnumDto>(TestContext.CancellationToken);

        Assert.AreEqual(MyEnum.A, dto!.A);
        Assert.AreEqual(MyEnum.B, dto.B);
        Assert.AreEqual(MyEnum.C, dto.C);
        Assert.AreEqual(MyEnum.D, dto.D);
    }

    #endregion Data

    #region Filtered

    [TestMethod]
    public async Task Many__Filtered_from_view_resultset()
    {
        var phorm = getPhormSession();
        await setupGetTestSchema(phorm);

        await phorm.CallAsync("GetTest_Upsert", null, TestContext.CancellationToken);
        await phorm.CallAsync("GetTest_Upsert", null, TestContext.CancellationToken);
        await phorm.CallAsync("GetTest_Upsert", null, TestContext.CancellationToken);

        var filter = phorm.From<IDataView>(null)
            .Where<DataItem>(o => o.Id == 1);
        var res = await filter.GetAllAsync(TestContext.CancellationToken);

        Assert.AreEqual(1, res.Single().Id);
    }

    [TestMethod]
    public async Task Many__Filtered_from_table_resultset()
    {
        var phorm = getPhormSession();
        await setupGetTestSchema(phorm);

        await phorm.CallAsync("GetTest_Upsert", null, TestContext.CancellationToken);
        await phorm.CallAsync("GetTest_Upsert", null, TestContext.CancellationToken);
        await phorm.CallAsync("GetTest_Upsert", null, TestContext.CancellationToken);

        var filter = phorm.From<DataItem>(null)
            .Where<DataItem>(o => o.Id == 1);
        var res = await filter.GetAllAsync(TestContext.CancellationToken);

        Assert.AreEqual(1, res.Single().Id);
    }

    [TestMethod]
    public async Task Many__Filtered_result_doesnt_resolve_unwanted()
    {
        var phorm = getPhormSession();
        await setupGetTestSchema(phorm);

        await phorm.CallAsync("GetTest_Upsert", new { Int = 10 }, TestContext.CancellationToken);
        await phorm.CallAsync("GetTest_Upsert", new { Int = 20 }, TestContext.CancellationToken);
        await phorm.CallAsync("GetTest_Upsert", new { Int = 30 }, TestContext.CancellationToken);
        await phorm.CallAsync("GetTest_Upsert", new { Int = 40 }, TestContext.CancellationToken);

        // Act
        var filter = phorm.From<IDataView>(null)
            .Where<DataItem>(o => o.Id <= 3 && o.Text == null && o.Num.HasValue && o.Num.Value > 10 && o.Flag);
        var res = await filter.GetAllAsync(TestContext.CancellationToken);

        Assert.IsTrue(hasUnresolvedEntities(res));
        Assert.HasCount(2, res);
        Assert.HasCount(2, res.ToArray()); // Resolve the entities
        Assert.IsFalse(hasUnresolvedEntities(res));
    }

    #endregion Filtered
}
