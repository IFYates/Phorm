using IFY.Phorm.Connectivity;
using IFY.Phorm.Data;
using IFY.Phorm.Transformation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System.Runtime.Serialization;

namespace IFY.Phorm.SqlClient.IntegrationTests
{
    [TestClass]
    public class GenSpecTests : SqlIntegrationTestBase
    {
        enum DataType { None, Numeric, String }

        abstract class BaseDataItem
        {
            public long Id { get; set; } = 0;
            public string Key { get; set; } = string.Empty;
            [DataMember(Name = "TypeId"), EnumValue]
            public DataType Type { get; set; } = DataType.None;
        }

        [PhormSpecOf(nameof(Type), DataType.Numeric)]
        class NumericDataItem : BaseDataItem
        {
            public decimal Number { get; set; } = 0m;
        }

        [PhormSpecOf(nameof(Type), DataType.String)]
        class TextDataItem : BaseDataItem
        {
            public string String { get; set; } = string.Empty;
        }

        private static void setupGenSpecContract(IPhormDbConnectionProvider connProv)
        {
            SqlTestHelpers.ApplySql(connProv, @"CREATE OR ALTER PROC [dbo].[usp_GetAllDataItems]
AS
	SELECT 1 [Id], 'Aaa' [Key], 1 [TypeId], 12.34 [Number], CONVERT(VARCHAR(50), NULL) [String]
	UNION ALL
	SELECT 2, 'Bbb', 2, NULL, 'Value'
RETURN 1");
        }

        [TestMethod]
        public void GenSpec__Can_retrieve_and_handle_many_types()
        {
            // Arrange
            var phorm = getPhormSession(out var connProv);
            setupGenSpecContract(connProv);

            // Act
            var res = phorm.From("GetAllDataItems")
                .Get<GenSpec<BaseDataItem, NumericDataItem, TextDataItem>>()!;

            var all = res.All();
            var nums = res.OfType<NumericDataItem>().ToArray();
            var strs = res.OfType<TextDataItem>().ToArray();

            // Assert
            Assert.AreEqual(2, all.Length);
            Assert.AreEqual(12.34m, nums.Single().Number);
            Assert.AreEqual("Value", strs.Single().String);
        }

        [TestMethod]
        public void GenSpec__Unknown_type_Abstract_base__Returns_only_shaped_items()
        {
            // Arrange
            var phorm = getPhormSession(out var connProv);
            setupGenSpecContract(connProv);

            // Act
            var res = phorm.From("GetAllDataItems")
                .Get<GenSpec<BaseDataItem, TextDataItem>>()!;

            var all = res.All();
            var strs = res.OfType<TextDataItem>().ToArray();

            // Assert
            Assert.AreEqual(1, all.Length);
            Assert.AreEqual(1, strs.Length);
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
        public void GenSpec__Unknown_type_Nonabstract_base__Returns_item_as_base()
        {
            // Arrange
            var phorm = getPhormSession(out var connProv);
            setupGenSpecContract(connProv);

            // Act
            var res = phorm.From("GetAllDataItems")
                .Get<GenSpec<BaseDataItemNonabstract, NumericDataItem2>>()!;

            var all = res.All();
            var asBase = all.Where(r => r.GetType() == typeof(BaseDataItemNonabstract)).ToArray();
            var nums = res.OfType<NumericDataItem2>().ToArray();

            // Assert
            Assert.AreEqual(2, all.Length);
            Assert.AreEqual(1, nums.Length);
            Assert.AreEqual(1, asBase.Length);
        }
    }
}
