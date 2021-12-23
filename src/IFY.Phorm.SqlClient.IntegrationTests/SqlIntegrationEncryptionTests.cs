using IFY.Phorm.Data;
using IFY.Phorm.Encryption;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace IFY.Phorm.SqlClient.IntegrationTests
{
    [TestClass]
    public class SqlIntegrationEncryptionTests
    {
        [PhormContract(Name = "DataTable")]
        public record DataItem(long Id, int Int, [property: SecureValue("class", nameof(DataItem.Int))] string Data)
        {
            public DataItem() : this(default, default, default!)
            { }
        }
        [PhormContract]
        public interface IUpsert : IPhormContract
        {
            int Int { get; }
            [SecureValue("class", nameof(DataItem.Int))]
            string Data { get; }
        }

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
            var res = phorm.CallAsync<IUpsert>(new { Int = randInt, Data = randStr }).Result;
            var obj = phorm.From("DataTable", objectType: DbObjectType.Table)
                .OneAsync<DataItem>().Result!;

            // Assert
            Assert.AreEqual(1, res);
            Assert.AreEqual(randStr, obj.Data);
            CollectionAssert.AreEqual(randInt.GetBytes(), provider.Encryptor.Authenticator);
        }
    }
}
