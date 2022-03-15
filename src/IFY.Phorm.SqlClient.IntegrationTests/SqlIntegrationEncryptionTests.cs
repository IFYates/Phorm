using IFY.Phorm.Data;
using IFY.Phorm.Encryption;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

namespace IFY.Phorm.SqlClient.IntegrationTests
{
    [TestClass]
    public class SqlIntegrationEncryptionTests
    {
        [PhormContract(Name = "DataTable")]
        public class DataItem
        {
            public long Id { get; set; }
            [DataMember(Name = "Int")] public int Num { get; set; }
            [SecureValue("class", nameof(Num))] public string Data { get; set; }

            public DataItem(long id, int num, string data)
            {
                Id = id;
                Num = num;
                Data = data;
            }

            public DataItem() : this(default, default, default!)
            { }
        }
        [PhormContract]
        public interface IUpsert : IPhormContract
        {
            [DataMember(Name = "Int")] int Num { get; }
            [SecureValue("class", nameof(Num))] string Data { get; }
        }

        [ExcludeFromCodeCoverage]
        private class TestEncryptionProvider : IEncryptionProvider
        {
            public IEncryptor Encryptor { get; } = new NullEncryptor();

            public IEncryptor GetInstance(string dataClassification)
            {
                return dataClassification switch
                {
                    "class" => Encryptor,
                    _ => throw new NotImplementedException(),
                };
            }
        }

        private static IPhormSession getPhormSession()
        {
            var connProc = new SqlConnectionProvider(@"Server=(localdb)\ProjectModels;Database=PhormTests;");

            var phorm = new SqlPhormSession(connProc, "*");

            phorm.Call("ClearTable");

            return phorm;
        }

        [TestMethod]
        public void String_encryption_full()
        {
            // Arrange
            var phorm = getPhormSession();

            var randInt = DateTime.UtcNow.Millisecond;
            var randStr = Guid.NewGuid().ToString();

            var provider = new TestEncryptionProvider();
            GlobalSettings.EncryptionProvider = provider;

            // Act
            var res = phorm.CallAsync<IUpsert>(new { Num = randInt, Data = randStr }).Result;
            var obj = phorm.GetAsync<DataItem>().Result!;

            // Assert
            Assert.AreEqual(1, res);
            Assert.AreEqual(randStr, obj.Data);
            CollectionAssert.AreEqual(randInt.GetBytes(), provider.Encryptor.Authenticator);
        }
    }
}
