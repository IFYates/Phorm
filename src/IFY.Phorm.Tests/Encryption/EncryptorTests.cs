using IFY.Phorm.Data;
using IFY.Phorm.Execution;
using IFY.Phorm.Tests;
using IFY.Phorm.Transformation;
using Moq;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace IFY.Phorm.Encryption.Tests;

[TestClass]
public class EncryptorTests
{
    public TestContext TestContext { get; set; }

    interface ISaveDataObject : IPhormContract
    {
        [SecureValue("Test")]
        string? Value { get; }
    }

    interface ISaveDataObjectWithAuthenticator : IPhormContract
    {
        string? Authenticator { get; }
        [SecureValue("Test", nameof(Authenticator))]
        string? Value { get; }
    }

    interface ISaveDataObjectWithTransformation : IPhormContract
    {
        [ReverseString]
        [SecureValue("Test")]
        string? Value { get; }
    }
    class ReverseStringAttribute : AbstractTransphormAttribute
    {
        [ExcludeFromCodeCoverage]
        public override object? FromDatasource(Type type, object? data, object? context)
        {
            throw new NotImplementedException();
        }

        public override object? ToDatasource(object? data, object? context)
        {
            return string.Join("", ((string?)data ?? "").AsEnumerable().Reverse());
        }
    }

    [TestMethod]
    public async Task Can_encrypt_string_value_out_to_database()
    {
        // Arrange
        var runner = new TestPhormSession();

        var args = new { Value = "value" };
        var objectData = Encoding.UTF8.GetBytes(args.Value);
        var secureData = new byte[] { 1, 2, 3, 4 };

        var encMock = new Mock<IEncryptor>();
        encMock.SetupProperty(m => m.Authenticator);
        encMock.Setup(m => m.Encrypt(objectData))
            .Returns(secureData);

        var encProcMock = new Mock<IEncryptionProvider>();
        encProcMock.Setup(m => m.GetEncryptor("Test"))
            .Returns(encMock.Object);

        GlobalSettings.EncryptionProvider = encProcMock.Object;

        // Act
        var res = await runner.CallAsync<ISaveDataObject>(args, TestContext.CancellationToken);

        // Assert
        Assert.AreEqual(1, res);
        Assert.IsEmpty(encMock.Object.Authenticator);

        var testCmd = runner.Commands.Single();
        Assert.AreSame(secureData, ((IDataParameter)testCmd.Parameters["@Value"]).Value);
    }

    [TestMethod]
    public async Task Can_encrypt_with_authenticator()
    {
        // Arrange
        var runner = new TestPhormSession();

        var args = new { Authenticator = "auth", Value = "value" };
        var authBytes = Encoding.UTF8.GetBytes(args.Authenticator);
        var objectData = Encoding.UTF8.GetBytes(args.Value);

        var encMock = new Mock<IEncryptor>();
        encMock.SetupProperty(m => m.Authenticator);
        encMock.Setup(m => m.Encrypt(objectData))
            .Returns([]);

        var encProcMock = new Mock<IEncryptionProvider>();
        encProcMock.Setup(m => m.GetEncryptor("Test"))
            .Returns(encMock.Object);

        GlobalSettings.EncryptionProvider = encProcMock.Object;

        // Act
        var res = await runner.CallAsync<ISaveDataObjectWithAuthenticator>(args, TestContext.CancellationToken);

        // Assert
        Assert.AreEqual(1, res);
        CollectionAssert.AreEqual(authBytes, encMock.Object.Authenticator);
    }

    [TestMethod]
    public async Task Can_encrypt_transformed_value()
    {
        // Arrange
        var runner = new TestPhormSession();

        var args = new { Value = "value" };
        var objectData = Encoding.UTF8.GetBytes("eulav"); // Transformed
        var secureData = new byte[] { 1, 2, 3, 4 };

        var encMock = new Mock<IEncryptor>();
        encMock.Setup(m => m.Encrypt(objectData))
            .Returns(secureData);

        var encProcMock = new Mock<IEncryptionProvider>();
        encProcMock.Setup(m => m.GetEncryptor("Test"))
            .Returns(encMock.Object);

        GlobalSettings.EncryptionProvider = encProcMock.Object;

        // Act
        var res = await runner.CallAsync<ISaveDataObjectWithTransformation>(args, TestContext.CancellationToken);

        // Assert
        Assert.AreEqual(1, res);

        var testCmd = runner.Commands.Single();
        Assert.AreSame(secureData, ((IDataParameter)testCmd.Parameters["@Value"]).Value);
    }
}
