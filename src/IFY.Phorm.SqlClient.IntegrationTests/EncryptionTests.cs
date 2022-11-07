using IFY.Phorm.Connectivity;
using IFY.Phorm.Data;
using IFY.Phorm.Encryption;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

namespace IFY.Phorm.SqlClient.IntegrationTests
{
    [TestClass]
    public class EncryptionTests : SqlIntegrationTestBase
    {
        [PhormContract(Name = "EncryptionTable", Target = DbObjectType.Table)]
        public class DataItem : IUpsert
        {
            public long Id { get; set; }
            [DataMember(Name = "Int")] public int Num { get; set; }
            [SecureValue("test", nameof(Num))] public string Data { get; set; } = string.Empty;
        }

        [PhormContract(Name = "Encryption_Upsert")]
        public interface IUpsert : IPhormContract
        {
            [DataMember(Name = "Int")] int Num { get; }
            [SecureValue("test", nameof(Num))] string Data { get; }
        }

        [ExcludeFromCodeCoverage]
        private class TestEncryptionProvider : IEncryptionProvider
        {
            public IEncryptor Encryptor { get; } = new NullEncryptor();

            public IEncryptor GetInstance(string dataClassification)
            {
                return dataClassification switch
                {
                    "test" => Encryptor,
                    _ => throw new NotImplementedException(),
                };
            }
        }

        private void setupEncryptionSchema(IPhormDbConnectionProvider connProv)
        {
            SqlTestHelpers.ApplySql(connProv, @"DROP TABLE IF EXISTS [dbo].[EncryptionTable]");
            SqlTestHelpers.ApplySql(connProv, @"CREATE TABLE [dbo].[EncryptionTable] (
	[Id] BIGINT NOT NULL IDENTITY(1,1) PRIMARY KEY,
	[Int] INT NULL,
	[Data] VARBINARY(MAX) NULL
)");

            SqlTestHelpers.ApplySql(connProv, @"CREATE OR ALTER PROC [dbo].[usp_Encryption_Upsert]
	@Id BIGINT = NULL OUTPUT,
	@Int INT = NULL,
	@Data VARBINARY(MAX) = NULL
AS
	SET NOCOUNT ON
	IF (@Id IS NULL) BEGIN
		INSERT [dbo].[EncryptionTable] ([Int], [Data])
			SELECT @Int, @Data
		SET @Id = SCOPE_IDENTITY()
		RETURN 1
	END

	UPDATE [dbo].[EncryptionTable] SET
		[Int] = @Int,
		[Data] = @Data
		WHERE [Id] = @Id
RETURN @@ROWCOUNT");
        }

        [TestMethod]
        public void String_encryption_full()
        {
            // Arrange
            var phorm = getPhormSession(out var connProv);
            setupEncryptionSchema(connProv);

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
