using IFY.Phorm.Data;
using IFY.Phorm.Execution;
using IFY.Phorm.Transformation;
using System.Runtime.Serialization;

namespace IFY.Phorm.SqlClient.IntegrationTests;

[TestClass]
public class GenSpecTests : SqlIntegrationTestBase
{
    enum DataType { None, Numeric, String }

    abstract record BaseDataItem(
        long Id,
        string Key,
        [property: DataMember(Name = "TypeId"), EnumValue] DataType Type
    );

    [PhormSpecOf(nameof(Type), DataType.Numeric)]
    record NumericDataItem(long Id, string Key, DataType Type, decimal Number) : BaseDataItem(Id, Key, Type);

    [PhormSpecOf(nameof(Type), DataType.String)]
    record TextDataItem(long Id, string Key, DataType Type, string String) : BaseDataItem(Id, Key, Type);

    private async Task setupGenSpecContract(AbstractPhormSession phorm)
    {
        await SqlTestHelpers.ApplySql(phorm, TestContext.CancellationToken, @"CREATE OR ALTER PROC [dbo].[usp_GetAllDataItems]
AS
	SELECT 1 [Id], 'Aaa' [Key], 1 [TypeId], 12.34 [Number], CONVERT(VARCHAR(50), NULL) [String]
	UNION ALL
	SELECT 2, 'Bbb', 2, NULL, 'Value'
RETURN 1");
    }

    [TestMethod]
    public async Task GenSpec__Can_retrieve_and_handle_many_types()
    {
        // Arrange
        var phorm = getPhormSession();
        await setupGenSpecContract(phorm);

        // Act
        var res = await phorm.From("GetAllDataItems", null)
            .GetAsync<GenSpec<BaseDataItem, NumericDataItem, TextDataItem>>(TestContext.CancellationToken);

        var all = res!.All();
        var nums = res.OfType<NumericDataItem>().ToArray();
        var strs = res.OfType<TextDataItem>().ToArray();

        // Assert
        Assert.HasCount(2, all);
        Assert.AreEqual(12.34m, nums.Single().Number);
        Assert.AreEqual("Value", strs.Single().String);
    }

    [TestMethod]
    public async Task GenSpec__Can_retrieve_and_handle_many_types_filtered()
    {
        // Arrange
        var phorm = getPhormSession();
        await setupGenSpecContract(phorm);

        // Act
        var res = await ((IPhormSession)phorm).From("GetAllDataItems", null)
            .Where<BaseDataItem, GenSpec<BaseDataItem, NumericDataItem, TextDataItem>>(o => o.Id == 1)
            .GetAllAsync(TestContext.CancellationToken);

        var nums = res.OfType<NumericDataItem>().ToArray();
        var strs = res.OfType<TextDataItem>().ToArray();
        var all = res.All();

        // Assert
        Assert.HasCount(1, all);
        Assert.AreEqual(12.34m, nums.Single().Number);
        Assert.HasCount(0, strs);
    }

    [TestMethod]
    public async Task GenSpec__Unknown_type_Abstract_base__Returns_only_shaped_items()
    {
        // Arrange
        var phorm = getPhormSession();
        await setupGenSpecContract(phorm);

        // Act
        var res = await phorm.From("GetAllDataItems", null)
            .GetAsync<GenSpec<BaseDataItem, TextDataItem>>(TestContext.CancellationToken);

        var all = res!.All();
        var strs = res.OfType<TextDataItem>().ToArray();

        // Assert
        Assert.HasCount(1, all);
        Assert.HasCount(1, strs);
    }

    class BaseDataItemNonabstract
    {
        public long Id { get; set; } = 0;
        public string Key { get; set; } = string.Empty;
        [DataMember(Name = "TypeId"), EnumValue]
        public DataType Type { get; set; } = DataType.None;
    }

    [PhormSpecOf(nameof(Type), DataType.Numeric)]
    class NumericDataItem2 : BaseDataItemNonabstract
    {
        public decimal Number { get; set; } = 0m;
    }

    [TestMethod]
    public async Task GenSpec__Unknown_type_Nonabstract_base__Returns_item_as_base()
    {
        // Arrange
        var phorm = getPhormSession();
        await setupGenSpecContract(phorm);

        // Act
        var res = await phorm.From("GetAllDataItems", null)
            .GetAsync<GenSpec<BaseDataItemNonabstract, NumericDataItem2>>(TestContext.CancellationToken);

        var all = res!.All();
        var asBase = all.Where(r => r.GetType() == typeof(BaseDataItemNonabstract)).ToArray();
        var nums = res.OfType<NumericDataItem2>().ToArray();

        // Assert
        Assert.HasCount(2, all);
        Assert.HasCount(1, nums);
        Assert.HasCount(1, asBase);
    }
}
