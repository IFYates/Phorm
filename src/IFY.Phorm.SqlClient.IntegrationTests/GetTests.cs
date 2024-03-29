using IFY.Phorm.Data;
using IFY.Phorm.Execution;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections;
using System.Reflection;
using System.Runtime.Serialization;

namespace IFY.Phorm.SqlClient.IntegrationTests;

[TestClass]
public class GetTests : SqlIntegrationTestBase
{
    [PhormContract(Name = "GetTestTable", Target = DbObjectType.Table)]
    public class DataItem : IUpsert, IUpsertOnlyIntWithId, IUpsertWithId
    {
        public long Id { get; set; }
        [DataMember(Name = "Int")]
        public int? Num { get; set; }
        public string? Text { get; set; }
        public byte[]? Data { get; set; }
        public DateTime? DateTime { get; set; }
        public bool Flag { get; set; } = true;

        public DataItem(long id, int? num, string? text, byte[]? data, DateTime? dateTime)
        {
            Id = id;
            Num = num;
            Text = text;
            Data = data;
            DateTime = dateTime;
        }
        public DataItem() : this(default, default, default, default, default)
        { }
    }

    [PhormContract(Name = "GetTestTable", Target = DbObjectType.Table)]
    public class DataItemWithoutText
    {
        public long Id { get; set; }
        [DataMember(Name = "Int")]
        public int? Num { get; set; }
        [IgnoreDataMember]
        public string? Text { get; set; }
        public byte[]? Data { get; set; }
        public DateTime? DateTime { get; set; }

        public DataItemWithoutText(long id, int? num, string? text, byte[]? data, DateTime? dateTime)
        {
            Id = id;
            Num = num;
            Text = text;
            Data = data;
            DateTime = dateTime;
        }
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

    private void setupGetTestSchema(AbstractPhormSession phorm)
    {
        SqlTestHelpers.ApplySql(phorm, @"DROP TABLE IF EXISTS [dbo].[GetTestTable]");
        SqlTestHelpers.ApplySql(phorm, @"CREATE TABLE [dbo].[GetTestTable] (
	[Id] BIGINT NOT NULL IDENTITY(1,1) PRIMARY KEY,
	[Int] INT NULL,
	[Text] VARCHAR(256) NULL,
	[Data] VARBINARY(MAX) NULL,
	[DateTime] DATETIME2 NULL,
	[IsInView] BIT NOT NULL DEFAULT (1)
)");

        SqlTestHelpers.ApplySql(phorm, @"CREATE OR ALTER VIEW [dbo].[vw_Data] AS SELECT * FROM [dbo].[GetTestTable] WHERE [IsInView] = 1");

        SqlTestHelpers.ApplySql(phorm, @"CREATE OR ALTER PROC [dbo].[usp_GetAll]
AS
	SET NOCOUNT ON
	SELECT * FROM [dbo].[GetTestTable]
RETURN 1");

        SqlTestHelpers.ApplySql(phorm, @"CREATE OR ALTER PROC [dbo].[usp_GetTest_Upsert]
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
RETURN @@ROWCOUNT");
    }

    #region Many

    [TestMethod]
    public void Many__Can_access_returnvalue_of_sproc()
    {
        var phorm = getPhormSession();
        setupGetTestSchema(phorm);

        ((IPhormSession)phorm).Call("GetTest_Upsert");
        ((IPhormSession)phorm).Call("GetTest_Upsert");
        ((IPhormSession)phorm).Call("GetTest_Upsert");
        ((IPhormSession)phorm).Call("GetTest_Upsert");

        var obj = new { ReturnValue = ContractMember.RetVal() };
        var x = phorm.From<IGetAll>(obj)
            .Get<DataItem[]>()!;

        Assert.AreEqual(1, obj.ReturnValue.Value);
        Assert.AreEqual(4, x.Length);
    }

    [TestMethod]
    public void Many__All_from_view()
    {
        // Arrange
        var phorm = getPhormSession();
        setupGetTestSchema(phorm);

        phorm.Call("GetTest_Upsert", new { Int = 0, IsInView = false });
        phorm.Call("GetTest_Upsert", new { Int = 0, IsInView = false });
        phorm.Call("GetTest_Upsert", new { Int = 1, IsInView = true });
        phorm.Call("GetTest_Upsert", new { Int = 1, IsInView = true });
        phorm.Call("GetTest_Upsert", new { Int = 1, IsInView = true });

        // Act
        var res = phorm.From<IDataView>(null)
            .Get<DataItem[]>()!;

        // Assert
        Assert.AreEqual(3, res.Length);
        Assert.IsTrue(res.All(e => e.Num == 1));
    }

    [TestMethod]
    public void Many__Filtered_from_view()
    {
        // Arrange
        var phorm = getPhormSession();
        setupGetTestSchema(phorm);

        var obj1 = new DataItem();
        var res1 = phorm.Call<IUpsertWithId>(obj1);

        var obj2 = new DataItem();
        var res2 = phorm.Call<IUpsertWithId>(obj2);

        // Act
        var res3 = phorm.From<IDataView>(new { obj2.Id })
            .Get<DataItem[]>()!;

        // Assert
        Assert.AreEqual(1, res1);
        Assert.AreEqual(1, res2);
        Assert.AreNotEqual(obj1.Id, obj2.Id);
        Assert.AreEqual(obj2.Id, res3.Single().Id);
    }

    [TestMethod]
    public void Many__All_from_table()
    {
        var phorm = getPhormSession();
        setupGetTestSchema(phorm);

        _ = phorm.Call("GetTest_Upsert", null);
        _ = phorm.Call("GetTest_Upsert", null);
        _ = phorm.Call("GetTest_Upsert", null);

        var res = phorm.Get<DataItemWithoutText[]>(null!)!;

        Assert.AreEqual(3, res.Length);
    }

    [TestMethod]
    public void Many__Filtered_from_table()
    {
        var phorm = getPhormSession();
        setupGetTestSchema(phorm);

        _ = phorm.Call("GetTest_Upsert", null);
        _ = phorm.Call("GetTest_Upsert", null);
        _ = phorm.Call("GetTest_Upsert", null);

        var x = phorm.Get<DataItem[]>(new { Id = 1 })!;

        Assert.AreEqual(1, x.Single().Id);
    }

    [TestMethod]
    public void Many__IEnumerable_resolves_later()
    {
        static bool hasUnresolvedEntities(IEnumerable l)
        {
            var resolversField = typeof(EntityList<DataItem>)
                .GetField("_resolvers", BindingFlags.Instance | BindingFlags.NonPublic)!;
            return ((ICollection)resolversField.GetValue(l)!).Count > 0;
        }

        var phorm = getPhormSession();
        setupGetTestSchema(phorm);

        _ = phorm.Call("GetTest_Upsert", null);
        _ = phorm.Call("GetTest_Upsert", null);
        _ = phorm.Call("GetTest_Upsert", null);

        var res = phorm.From<IDataView>(null).Get<IEnumerable<DataItem>>()!;

        Assert.IsTrue(hasUnresolvedEntities(res));
        Assert.AreEqual(3, res.Count());
        Assert.IsTrue(hasUnresolvedEntities(res));
        Assert.AreEqual(3, res.ToArray().Length);
        Assert.IsFalse(hasUnresolvedEntities(res));
    }

    [TestMethod]
    public void Many__ICollection_resolves_later()
    {
        static bool hasUnresolvedEntities(IEnumerable l)
        {
            var resolversField = typeof(EntityList<DataItem>)
                .GetField("_resolvers", BindingFlags.Instance | BindingFlags.NonPublic)!;
            return ((ICollection)resolversField.GetValue(l)!).Count > 0;
        }

        var phorm = getPhormSession();
        setupGetTestSchema(phorm);

        _ = phorm.Call("GetTest_Upsert", null);
        _ = phorm.Call("GetTest_Upsert", null);
        _ = phorm.Call("GetTest_Upsert", null);

        var res = phorm.From<IDataView>(null).Get<ICollection<DataItem>>()!;

        Assert.IsTrue(hasUnresolvedEntities(res));
        Assert.AreEqual(3, res.Count);
        Assert.IsTrue(hasUnresolvedEntities(res));
        Assert.AreEqual(3, res.ToArray().Length);
        Assert.IsFalse(hasUnresolvedEntities(res));
    }

    #endregion Many

    #region One

    [TestMethod]
    public void One__Can_ignore_property()
    {
        var phorm = getPhormSession();
        setupGetTestSchema(phorm);

        var res = phorm.Call("GetTest_Upsert", null);

        var obj = phorm.From<IDataView>(new { Id = 1 })
            .Get<DataItemWithoutText>()!;

        Assert.AreEqual(1, res);
        Assert.IsNull(obj.Text);
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
    public void Data__Supports_all_numeric_types_for_enum()
    {
        var phorm = getPhormSession();

        SqlTestHelpers.ApplySql(phorm, @"DROP TABLE IF EXISTS [dbo].[GetTestTable]");
        SqlTestHelpers.ApplySql(phorm, @"CREATE TABLE [dbo].[GetTestTable] (
	[A] TINYINT,
	[B] SMALLINT,
	[C] INT,
	[D] BIGINT
)");
        SqlTestHelpers.ApplySql(phorm, @"INSERT INTO [dbo].[GetTestTable] VALUES (1, 2, 3, 4)");

        var dto = ((IPhormSession)phorm).Get<EnumDto>()!;

        Assert.AreEqual(MyEnum.A, dto.A);
        Assert.AreEqual(MyEnum.B, dto.B);
        Assert.AreEqual(MyEnum.C, dto.C);
        Assert.AreEqual(MyEnum.D, dto.D);
    }

    #endregion Data

    #region Filtered

    [TestMethod]
    [DataRow(false), DataRow(true)]
    public void Many__Filtered_from_view_resultset(bool byAsync)
    {
        var phorm = getPhormSession();
        setupGetTestSchema(phorm);

        _ = phorm.Call("GetTest_Upsert", null);
        _ = phorm.Call("GetTest_Upsert", null);
        _ = phorm.Call("GetTest_Upsert", null);

        var filter = phorm.From<IDataView>(null)
            .Where<DataItem>(o => o.Id == 1);
        var res = byAsync ? filter.GetAllAsync().Result : filter.GetAll();

        Assert.AreEqual(1, res.Single().Id);
    }

    [TestMethod]
    [DataRow(false), DataRow(true)]
    public void Many__Filtered_from_table_resultset(bool byAsync)
    {
        var phorm = getPhormSession();
        setupGetTestSchema(phorm);

        _ = phorm.Call("GetTest_Upsert", null);
        _ = phorm.Call("GetTest_Upsert", null);
        _ = phorm.Call("GetTest_Upsert", null);

        var filter = phorm.From<DataItem>(null)
            .Where<DataItem>(o => o.Id == 1);
        var res = byAsync ? filter.GetAllAsync().Result : filter.GetAll();

        Assert.AreEqual(1, res.Single().Id);
    }

    [TestMethod]
    [DataRow(false), DataRow(true)]
    public void Many__Filtered_result_doesnt_resolve_unwanted(bool byAsync)
    {
        static bool hasUnresolvedEntities(IEnumerable l)
        {
            var resolversField = typeof(EntityList<DataItem>)
                .GetField("_resolvers", BindingFlags.Instance | BindingFlags.NonPublic)!;
            return ((ICollection)resolversField.GetValue(l)!).Count > 0;
        }

        var phorm = getPhormSession();
        setupGetTestSchema(phorm);

        _ = phorm.Call("GetTest_Upsert", new { Int = 10 });
        _ = phorm.Call("GetTest_Upsert", new { Int = 20 });
        _ = phorm.Call("GetTest_Upsert", new { Int = 30 });
        _ = phorm.Call("GetTest_Upsert", new { Int = 40 });

        // Act
        var filter = phorm.From<IDataView>(null)
            .Where<DataItem>(o => o.Id <= 3 && o.Text == null && o.Num.HasValue && o.Num.Value > 10 && o.Flag);
        var res = byAsync ? filter.GetAllAsync().Result : filter.GetAll();

        Assert.IsTrue(hasUnresolvedEntities(res));
        Assert.AreEqual(2, res.Count());
        Assert.AreEqual(2, res.ToArray().Length);
        Assert.IsFalse(hasUnresolvedEntities(res));
    }

    #endregion Filtered
}
