using IFY.Phorm.Data;
using IFY.Phorm.Encryption;
using IFY.Phorm.Execution;
using IFY.Phorm.Transformation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;

namespace IFY.Phorm.Tests.Encryption
{
    [TestClass]
    public class EncryptorTests
    {
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
                return string.Join("", ((string?)data ?? "").ToArray().Reverse());
            }
        }

        [TestInitialize]
        public void Init()
        {
            AbstractPhormSession.ResetConnectionPool();
        }

        [TestMethod]
        public void Can_encrypt_string_value_out_to_database()
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
            encProcMock.Setup(m => m.GetInstance("Test"))
                .Returns(encMock.Object);

            GlobalSettings.EncryptionProvider = encProcMock.Object;

            // Act
            var res = runner.Call<ISaveDataObject>(args);

            // Assert
            Assert.AreEqual(1, res);
            Assert.AreEqual(0, encMock.Object.Authenticator.Length);

            var testCmd = runner.Commands.Single();
            Assert.AreSame(secureData, ((IDataParameter)testCmd.Parameters["@Value"]).Value);
        }

        [TestMethod]
        public void Can_encrypt_with_authenticator()
        {
            // Arrange
            var runner = new TestPhormSession();

            var args = new { Authenticator = "auth", Value = "value" };
            var authBytes = Encoding.UTF8.GetBytes(args.Authenticator);
            var objectData = Encoding.UTF8.GetBytes(args.Value);

            var encMock = new Mock<IEncryptor>();
            encMock.SetupProperty(m => m.Authenticator);
            encMock.Setup(m => m.Encrypt(objectData))
                .Returns(Array.Empty<byte>());

            var encProcMock = new Mock<IEncryptionProvider>();
            encProcMock.Setup(m => m.GetInstance("Test"))
                .Returns(encMock.Object);

            GlobalSettings.EncryptionProvider = encProcMock.Object;

            // Act
            var res = runner.Call<ISaveDataObjectWithAuthenticator>(args);

            // Assert
            Assert.AreEqual(1, res);
            CollectionAssert.AreEqual(authBytes, encMock.Object.Authenticator);
        }

        [TestMethod]
        public void Can_encrypt_transformed_value()
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
            encProcMock.Setup(m => m.GetInstance("Test"))
                .Returns(encMock.Object);

            GlobalSettings.EncryptionProvider = encProcMock.Object;

            // Act
            var res = runner.Call<ISaveDataObjectWithTransformation>(args);

            // Assert
            Assert.AreEqual(1, res);

            var testCmd = runner.Commands.Single();
            Assert.AreSame(secureData, ((IDataParameter)testCmd.Parameters["@Value"]).Value);
        }
    }
}
