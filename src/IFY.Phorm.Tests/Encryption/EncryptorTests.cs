using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using IFY.Phorm.Encryption;
using IFY.Phorm.Transformation;
using System;
using System.Data;
using System.Linq;
using System.Text;

namespace IFY.Phorm.Tests.Encryption
{
    [TestClass]
    public class EncryptorTests
    {
        [PhormContract]
        class DataObject
        {
            public string? Authenticator { get; set; }
            public string? Value { get; set; }
        }

        [PhormContract]
        interface ISaveDataObject
        {
            [SecureValue("Test")]
            string? Value { get; }
        }

        [PhormContract]
        interface ISaveDataObjectWithAuthenticator
        {
            string? Authenticator { get; }
            [SecureValue("Test", nameof(Authenticator))]
            string? Value { get; }
        }

        [PhormContract]
        interface ISaveDataObjectWithTransformation
        {
            [ReverseString]
            [SecureValue("Test")]
            string? Value { get; }
        }
        class ReverseStringAttribute : AbstractTransphormAttribute
        {
            public override object? FromDatasource(Type type, object? data)
            {
                throw new NotImplementedException();
            }

            public override object? ToDatasource(object? data)
            {
                return string.Join("", ((string?)data ?? "").ToArray().Reverse());
            }
        }

        [TestMethod]
        public void Can_encrypt_string_value_out_to_database()
        {
            // Arrange
            var runner = new TestPhormRunner();

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
            var runner = new TestPhormRunner();

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
            var runner = new TestPhormRunner();

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
