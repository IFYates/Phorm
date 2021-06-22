using Microsoft.VisualStudio.TestTools.UnitTesting;
using IFY.Phorm.Data;
using IFY.Phorm.Encryption;
using IFY.Phorm.SqlClient;
using IFY.Phorm.Transformation;
using System;
using System.Runtime.Serialization;
using System.Linq;

namespace IFY.Phorm.Tests
{
    [TestClass]
    public class AbstractPhormRunnerTests
    {
        [DataContract]
        public record DTO(long Id, ETest Enum, DateTime? Timestamp) : IDataContract, ITest
        {
            [SecureValue("Test", nameof(Id))] public string SecureData { get; set; }
            public string? Res { get; set; }

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
        public interface ITest
        {
            long Id { get; }
            [EnumValue(SendAsString = true)]
            ETest Enum { get; }
            string Res { set; }
            //[SecureValue("Test", nameof(Id))] string SecureData { get; }
        }

        [PhormContract]
        public interface ITest2
        {
            long BattleId { get; }
        }

        [PhormContract]
        public interface IDataContract
        {
            long Id { get; }

            [SecureValue("Test", nameof(Id))] string SecureData { get; }

            [CalculatedValue]
            public string SecureDataHash() { return SecureData; }
        }

        private static IPhormRunner getPhormRunner()
        {
            var connProc = new SqlConnectionProvider(@"Server=(localdb)\ProjectsV13;Database=IFY.LittleEmpire.Database;");

            var phorm = new SqlPhormRunner(connProc, "*");

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
        public void Call()
        {
            var phorm = getPhormRunner();

            GlobalSettings.EncryptionProvider = new TestEncryptionProvider();

            var obj = new DTO(20, ETest.ValueX, null)
            {
                SecureData = "data"
            };
            var x = phorm.Call<ITest>(obj);
            Assert.AreEqual(1, x);
            Assert.IsNotNull(obj.Res);

            var obj2 = new { Id = 20, Res = ContractMember.Out<string>("Res") };
            var y = phorm.CallAsync<ITest>(obj2);
            Assert.AreEqual(1, y.Result);
            Assert.IsNotNull(obj2.Res.Value);
        }

        [TestMethod]
        public void All()
        {
            var phorm = getPhormRunner();

            var obj = new { BattleId = 1, ReturnValue = ContractMember.RetVal() };
            var x = phorm.All<DTO, ITest2>(obj);
            Assert.AreEqual(1, obj.ReturnValue.Value);
            Assert.AreEqual(4, x.Length);
        }
        
        [PhormContract(Name = "Battle", Target = DbObjectType.View)]
        public interface IBattleView
        {
            long? Id { get; }
        }

        [TestMethod]
        public void All_from_view()
        {
            var phorm = getPhormRunner();

            var x = phorm.All<DTO, IBattleView>(new { Id = 1 });
            Assert.AreEqual(1, x.Single().Id);
        }

        [PhormContract(Name = "Battle", Target = DbObjectType.Table)]
        public interface IBattleTable
        {
            long? Id { get; }
        }

        [TestMethod]
        public void All_from_table()
        {
            var phorm = getPhormRunner();

            var x = phorm.All<DTO, IBattleTable>(new { Id = 1 });
            Assert.AreEqual(1, x.Single().Id);
        }
    }
}
