using IFY.Phorm.Connectivity;
using IFY.Phorm.Data;
using IFY.Phorm.Transformation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.Serialization;

namespace IFY.Phorm.SqlClient.IntegrationTests
{
    [TestClass]
    public class GenSpecTests : SqlIntegrationTestBase
    {
        [PhormContract(Name = "DataTable")]
        [ExcludeFromCodeCoverage]
        public class DataItem
        {
            public long Id { get; set; }
            [DataMember(Name = "Int")]
            public int? Num { get; set; }
            public string? Text { get; set; }
            public byte[]? Data { get; set; }
            public DateTime? DateTime { get; set; }

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

        [PhormContract(Name = "DataTable")]
        [ExcludeFromCodeCoverage]
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
            public DataItemWithoutText() : this(default, default, default, default, default)
            { }
        }

        public enum DataType { None, Numeric, String }

        public abstract class BaseDataItem
        {
            public long Id { get; set; }
            public string Key { get; set; } = string.Empty;
            [DataMember(Name = "TypeId"), EnumValue]
            public DataType Type { get; set; }
        }

        [PhormSpecOf(nameof(Type), DataType.Numeric)]
        public class NumericDataItem : BaseDataItem
        {
            public decimal Number { get; set; }
        }

        [PhormSpecOf(nameof(Type), DataType.String)]
        public class TextDataItem : BaseDataItem
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

        public class BaseDataItemNonabstract
        {
            public long Id { get; set; }
            public string Key { get; set; } = string.Empty;
            [DataMember(Name = "TypeId"), EnumValue]
            public DataType Type { get; set; }
        }

        [PhormSpecOf(nameof(Type), DataType.Numeric)]
        public class NumericDataItem2 : BaseDataItemNonabstract
        {
            public decimal Number { get; set; }
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
