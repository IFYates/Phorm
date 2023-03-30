using IFY.Phorm.Data;
using IFY.Phorm.Encryption;
using IFY.Phorm.Execution;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Runtime.Serialization;

namespace IFY.Phorm.SqlClient.IntegrationTests;

[TestClass]
public class EncryptionTests : SqlIntegrationTestBase
{
    [PhormContract(Name = "EncryptionTable", Target = DbObjectType.Table)]
    class DataItem : IUpsert
    {
        public long Id { get; set; }
        [DataMember(Name = "Int")] public int Num { get; set; }
        [SecureValue("test", nameof(Num))] public string Data { get; set; } = string.Empty;
    }

    [PhormContract(Name = "Encryption_Upsert")]
    interface IUpsert : IPhormContract
    {
        [DataMember(Name = "Int")] int Num { get; }
        [SecureValue("test", nameof(Num))] string Data { get; }
    }

    private void setupEncryptionSchema(AbstractPhormSession phorm)
    {
        SqlTestHelpers.ApplySql(phorm, @"DROP TABLE IF EXISTS [dbo].[EncryptionTable]");
        SqlTestHelpers.ApplySql(phorm, @"CREATE TABLE [dbo].[EncryptionTable] (
	[Id] BIGINT NOT NULL IDENTITY(1,1) PRIMARY KEY,
	[Int] INT NULL,
	[Data] VARBINARY(MAX) NULL
)");

        SqlTestHelpers.ApplySql(phorm, @"CREATE OR ALTER PROC [dbo].[usp_Encryption_Upsert]
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
        var phorm = getPhormSession();
        setupEncryptionSchema(phorm);

        var randInt = DateTime.UtcNow.Millisecond;
        var randStr = Guid.NewGuid().ToString();
        var decdata = randStr.GetBytes();
        var encdata = Guid.NewGuid().ToString().GetBytes();

        var decryptor = new Mock<IEncryptor>(MockBehavior.Strict);
        decryptor.SetupAllProperties();
        decryptor.Setup(m => m.Decrypt(encdata))
            .Returns(decdata);

        var encryptor = new Mock<IEncryptor>(MockBehavior.Strict);
        encryptor.SetupAllProperties();
        encryptor.Setup(m => m.Encrypt(decdata))
            .Returns(encdata);

        var provider = new Mock<IEncryptionProvider>(MockBehavior.Strict);
        provider.Setup(m => m.GetEncryptor("test"))
            .Returns(encryptor.Object);
        provider.Setup(m => m.GetDecryptor("test", encdata))
            .Returns(decryptor.Object);
        GlobalSettings.EncryptionProvider = provider.Object;

        // Act
        var res = phorm.CallAsync<IUpsert>(new { Num = randInt, Data = randStr }, CancellationToken.None).Result;
        var obj = phorm.GetAsync<DataItem>(null, CancellationToken.None).Result!;

        // Assert
        Assert.AreEqual(1, res);
        Assert.AreEqual(randStr, obj.Data);
        CollectionAssert.AreEqual(randInt.GetBytes(), decryptor.Object.Authenticator);
        CollectionAssert.AreEqual(randInt.GetBytes(), encryptor.Object.Authenticator);
    }
}
