using IFY.Phorm.Data;
using IFY.Phorm.Encryption;
using IFY.Phorm.SqlClient;
using IFY.Phorm.Transformation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace IFY.Phorm.Tests
{
    [TestClass]
    public class SqlIntegrationTests
    {
        [PhormContract(Name = "DataTable")]
        public record DataItem(long Id, int? Int, string? Text, byte[]? Data, DateTime? DateTime)
            : IUpsert, IUpsertOnlyIntWithId, IUpsertWithId
        {
            public DataItem() : this(default, default, default, default, default)
            { }
        }

        [PhormContract(Name = "DataTable")]
        public record DataItemWithoutText(long Id, int? Int, [property: IgnoreDataMember] string? Text, byte[]? Data, DateTime? DateTime)
        {
            public DataItemWithoutText() : this(default, default, default, default, default)
            { }
        }

        [DataContract]
        public record DTO(long Id, ETest Enum, DateTime? Timestamp) : IDataContract, ITest
        {
            [DataMember(Name = "Data"), SecureValue("Test", nameof(Id))] public string SecureData { get; set; }
            public string? Text { get; set; }

            public DTO()
                : this(0, ETest.Value1, null)
            {
            }
        }

        public enum ETest
        {
            Value1 = 1,
            [EnumMember(Value = "Value2")]
            ValueX,
            Value3
        }

        [PhormContract]
        public interface IUpsert : IPhormContract
        {
            int? Int { get; }
            string? Text { get; }
            byte[]? Data { get; }
            DateTime? DateTime { get; }
        }
        [PhormContract(Name = "Upsert")]
        public interface IUpsertWithId : IUpsert
        {
            long Id { init; }
        }

        [PhormContract]
        public interface IGetAll : IPhormContract
        {
            long Id { get; }
            int? Int { get; }
            string? Text { get; }
            byte[]? Data { get; }
            DateTime? DateTime { get; }
        }

        [PhormContract(Name = "Upsert")]
        public interface IUpsertOnlyIntWithId : IPhormContract
        {
            long Id { init; }
            int? Int { get; }
        }

        [PhormContract]
        public interface ITest : IPhormContract
        {
            long Id { get; }
            [EnumValue(SendAsString = true)]
            ETest Enum { get; }
            string Text { set; }
            //[SecureValue("Test", nameof(Id))] string SecureData { get; }
        }

        [PhormContract]
        public interface ITest2 : IPhormContract
        {
            long Id { get; }
        }

        [PhormContract]
        public interface IDataContract : IPhormContract
        {
            long Id { get; }

            [DataMember(Name = "Data"), SecureValue("Test", nameof(Id))] string SecureData { get; }

            [CalculatedValue] // e.g., SecureDataHash
            public string Text() { return SecureData; }
        }

        private static IPhormRunner getPhormRunner()
        {
            var connProc = new SqlConnectionProvider(@"Server=(localdb)\ProjectModels;Database=PhormTests;");

            var phorm = new SqlPhormRunner(connProc, "*");

            phorm.From("ClearTable").Call();

            return phorm;
        }


        class TestEncryptionProvider : IEncryptionProvider
        {
            public IEncryptor GetInstance(string dataClassification)
            {
                return new NullEncryptor();
            }
        }

        [PhormContract(Name = "Data", Target = DbObjectType.View)]
        public interface IDataView : IPhormContract
        {
            long? Id { get; }
        }

        [PhormContract(Target = DbObjectType.Table)]
        public interface IDataTable : IPhormContract
        {
            long? Id { get; }
        }

        #region Call

        [TestMethod]
        public void Call__By_anon_Insert_various_types()
        {
            // Arrange
            var phorm = getPhormRunner();

            var randNum = DateTime.UtcNow.Millisecond;
            var randStr = Guid.NewGuid().ToString();
            var randData = Encoding.UTF8.GetBytes(Guid.NewGuid().ToString());
            var randDT = DateTime.UtcNow;

            // Act
            var res = phorm.Call("Upsert", new { Int = randNum, Text = randStr, Data = randData, DateTime = randDT });
            var obj = phorm.From("DataTable", objectType: DbObjectType.Table).One<DataItem>();

            // Assert
            Assert.AreEqual(1, res);
            Assert.AreEqual(randNum, obj.Int);
            Assert.AreEqual(randStr, obj.Text);
            CollectionAssert.AreEqual(randData, obj.Data);
            Assert.AreEqual(randDT, obj.DateTime);
        }

        [TestMethod]
        public void Call__By_contract_and_anon_arg_Insert_various_types()
        {
            // Arrange
            var phorm = getPhormRunner();

            var randNum = DateTime.UtcNow.Millisecond;
            var randStr = Guid.NewGuid().ToString();
            var randData = Encoding.UTF8.GetBytes(Guid.NewGuid().ToString());
            var randDT = DateTime.UtcNow;

            // Act
            var res = phorm.From<IUpsert>().Call(new { Int = randNum, Text = randStr, Data = randData, DateTime = randDT });
            var obj = phorm.From("DataTable", objectType: DbObjectType.Table).One<DataItem>();

            // Assert
            Assert.AreEqual(1, res);
            Assert.AreEqual(randNum, obj.Int);
            Assert.AreEqual(randStr, obj.Text);
            CollectionAssert.AreEqual(randData, obj.Data);
            Assert.AreEqual(randDT, obj.DateTime);
        }

        [TestMethod]
        public void Call__By_contract_arg_Insert_various_types()
        {
            // Arrange
            var phorm = getPhormRunner();

            var arg = new DataItem(0,
                DateTime.UtcNow.Millisecond,
                Guid.NewGuid().ToString(),
                Encoding.UTF8.GetBytes(Guid.NewGuid().ToString()),
                DateTime.UtcNow
            );

            // Act
            var res = phorm.Call<IUpsert>(arg);
            var obj = phorm.From("DataTable", objectType: DbObjectType.Table).One<DataItem>();

            // Assert
            Assert.AreEqual(1, res);
            Assert.AreEqual(arg.Int, obj.Int);
            Assert.AreEqual(arg.Text, obj.Text);
            CollectionAssert.AreEqual(arg.Data, obj.Data);
            Assert.AreEqual(arg.DateTime, obj.DateTime);
        }

        [TestMethod]
        public void Call__Get_by_anon_output()
        {
            // Arrange
            var phorm = getPhormRunner();

            var arg = new
            {
                Id = ContractMember.Out<long>()
            };

            // Act
            var res = phorm.From("Upsert").Call(arg);
            var obj = phorm.From("DataTable", objectType: DbObjectType.Table).One<DataItem>();

            // Assert
            Assert.AreEqual(1, res);
            Assert.AreEqual(obj.Id, arg.Id.Value);
        }

        [TestMethod]
        public void Call__Get_by_contract_output()
        {
            // Arrange
            var phorm = getPhormRunner();

            var arg = new DataItem();

            // Act
            var res = phorm.From<IUpsertOnlyIntWithId>().Call(arg);
            var obj = phorm.From("DataTable", objectType: DbObjectType.Table).One<DataItem>();

            // Assert
            Assert.AreEqual(1, res);
            Assert.AreEqual(obj.Id, arg.Id);
        }

        [TestMethod]
        public void Callx()
        {
            var phorm = getPhormRunner();

            GlobalSettings.EncryptionProvider = new TestEncryptionProvider();

            var obj = new DTO(20, ETest.ValueX, null)
            {
                SecureData = "data"
            };
            var x = phorm.From<ITest>().Call(obj);
            Assert.AreEqual(1, x);
            Assert.IsNotNull(obj.SecureData);

            var obj2 = new { Id = 20, Res = ContractMember.Out<string>("Res") };
            var y = phorm.From<ITest>().CallAsync(obj2);
            Assert.AreEqual(1, y.Result);
            Assert.IsNotNull(obj2.Res.Value);
        }

        #endregion Call

        #region Many

        [TestMethod]
        public void Many__Can_access_returnvalue_of_sproc()
        {
            var phorm = getPhormRunner();

            phorm.Call("Upsert");
            phorm.Call("Upsert");
            phorm.Call("Upsert");
            phorm.Call("Upsert");

            var obj = new { ReturnValue = ContractMember.RetVal() };
            var x = phorm.From<IGetAll>().Many<DataItem>(obj);

            Assert.AreEqual(1, obj.ReturnValue.Value);
            Assert.AreEqual(4, x.Length);
        }

        [TestMethod]
        public void Many__Filtered_from_view()
        {
            // Arrange
            var phorm = getPhormRunner();

            var obj1 = new DataItem();
            var res1 = phorm.Call<IUpsertWithId>(obj1);

            var obj2 = new DataItem();
            var res2 = phorm.Call<IUpsertWithId>(obj2);

            // Act
            var res3 = phorm.From<IDataView>().Many<DataItem>(new { obj2.Id });

            // Assert
            Assert.AreEqual(1, res1);
            Assert.AreEqual(1, res2);
            Assert.AreNotEqual(obj1.Id, obj2.Id);
            Assert.AreEqual(obj2.Id, res3.Single().Id);
        }

        [TestMethod]
        public void Many__Filtered_by_view()
        {
            // Arrange
            var phorm = getPhormRunner();

            phorm.Call("Upsert", new { Int = 0, IsInView = false });
            phorm.Call("Upsert", new { Int = 0, IsInView = false });
            phorm.Call("Upsert", new { Int = 1, IsInView = true });
            phorm.Call("Upsert", new { Int = 1, IsInView = true });
            phorm.Call("Upsert", new { Int = 1, IsInView = true });

            // Act
            var res = phorm.From<IDataView>().Many<DataItem>();

            // Assert
            Assert.AreEqual(3, res.Length);
            Assert.IsTrue(res.All(e => e.Int == 1));
        }

        [TestMethod]
        public void Many__Filtered_from_table()
        {
            var phorm = getPhormRunner();

            var res = phorm.Call("Upsert");

            var x = phorm.From<IDataView>().Many<DataItem>(new { Id = 1 });

            Assert.AreEqual(1, res);
            Assert.AreEqual(1, x.Single().Id);
        }

        #endregion Many

        #region One

        [TestMethod]
        public void One__Can_ignore_property()
        {
            var phorm = getPhormRunner();

            var res = phorm.Call("Upsert");

            var obj = phorm.From<IDataView>().One<DataItemWithoutText>(new { Id = 1 });

            Assert.AreEqual(1, res);
            Assert.IsNull(obj.Text);
        }

        #endregion One
    }
}
