using Microsoft.VisualStudio.TestTools.UnitTesting;
using IFY.Phorm.Data;
using IFY.Phorm.Encryption;
using IFY.Phorm.SqlClient;
using IFY.Phorm.Transformation;
using System;
using System.Runtime.Serialization;
using System.Linq;
using System.Text;

namespace IFY.Phorm.Tests
{
    [TestClass]
    public class SqlIntegrationTests
    {
        [PhormContract(Name = "DataTable")]
        public record DataItem(long Id, int? Int, string? Text, byte[]? Data, DateTime? DateTime)
            : IUpsert
        {
            public DataItem() : this(default, default, default, default, default)
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

            phorm.Call("ClearTable");

            return phorm;
        }


        class TestEncryptionProvider : IEncryptionProvider
        {
            public IEncryptor GetInstance(string dataClassification)
            {
                return new NullEncryptor();
            }
        }

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
            var obj = phorm.One<DataItem>("DataTable", objectType: DbObjectType.Table);

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
            var res = phorm.Call<IUpsert>(new { Int = randNum, Text = randStr, Data = randData, DateTime = randDT });
            var obj = phorm.One<DataItem>("DataTable", objectType: DbObjectType.Table);

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
            var obj = phorm.One<DataItem>("DataTable", objectType: DbObjectType.Table);

            // Assert
            Assert.AreEqual(1, res);
            Assert.AreEqual(arg.Int, obj.Int);
            Assert.AreEqual(arg.Text, obj.Text);
            CollectionAssert.AreEqual(arg.Data, obj.Data);
            Assert.AreEqual(arg.DateTime, obj.DateTime);
        }

        [TestMethod]
        public void Call__Get_output()
        {
            // Arrange
            var phorm = getPhormRunner();

            var arg = new
            {
                Id = ContractMember.Out<long>()
            };

            // Act
            var res = phorm.Call("Upsert", arg);
            var obj = phorm.One<DataItem>("DataTable", objectType: DbObjectType.Table);

            // Assert
            Assert.AreEqual(1, res);
            Assert.AreEqual(obj.Id, arg.Id.Value);
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
            var x = phorm.Call<ITest>(obj);
            Assert.AreEqual(1, x);
            Assert.IsNotNull(obj.SecureData);

            var obj2 = new { Id = 20, Res = ContractMember.Out<string>("Res") };
            var y = phorm.CallAsync<ITest>(obj2);
            Assert.AreEqual(1, y.Result);
            Assert.IsNotNull(obj2.Res.Value);
        }

        [TestMethod]
        public void All()
        {
            var phorm = getPhormRunner();

            var obj = new { Id = 1, ReturnValue = ContractMember.RetVal() };
            var x = phorm.Many<DTO, ITest2>(obj);
            Assert.AreEqual(1, obj.ReturnValue.Value);
            Assert.AreEqual(4, x.Length);
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

        [TestMethod]
        public void All_from_view()
        {
            var phorm = getPhormRunner();

            phorm.Call("Upsert");

            var x = phorm.Many<DataItem, IDataView>(new { Id = 1 });
            Assert.AreEqual(1, x.Single().Id);
        }

        [TestMethod]
        public void All_from_table()
        {
            var phorm = getPhormRunner();
            
            phorm.Call("Upsert");

            var x = phorm.Many<DataItem, IDataTable>(new { Id = 1 });
            Assert.AreEqual(1, x.Single().Id);
        }
    }
}
