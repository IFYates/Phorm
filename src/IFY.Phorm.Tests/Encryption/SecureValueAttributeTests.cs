using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;

namespace IFY.Phorm.Encryption.Tests
{
    [TestClass]
    public class SecureValueAttributeTests
    {
        public class ClassWithAuthenticator
        {
            public long AuthenticatorValue { get; set; }
        }

        [TestMethod]
        public void Decrypt__Null__Empty()
        {
            // Arrange
            var attr = new SecureValueAttribute("class", null);

            // Act
            var res = attr.Decrypt(null);

            // Assert
            Assert.AreEqual(0, res.Length);
        }

        [TestMethod]
        public void Decrypt__Null_provider__Exception()
        {
            // Arrange
            var attr = new SecureValueAttribute("class", null);

            GlobalSettings.EncryptionProvider = null;

            // Act
            Assert.ThrowsException<NullReferenceException>(() =>
            {
                _ = attr.Decrypt(new byte[] { 1, 2, 3, 4 });
            });
        }

        [TestMethod]
        public void Decrypt__Provider_gives_null_encryptor__NOOP()
        {
            // Arrange
            var attr = new SecureValueAttribute("class", null);

            var provMock = new Mock<IEncryptionProvider>(MockBehavior.Strict);
            provMock.Setup(m => m.GetInstance("class"))
                .Returns(() => null!);
            GlobalSettings.EncryptionProvider = provMock.Object;

            var data = new byte[] { 1, 2, 3, 4 };

            // Act
            var res = attr.Decrypt(data);

            // Assert
            Assert.AreSame(data, res);
        }

        [TestMethod]
        public void Decrypt__Decrypts_by_encryptor()
        {
            // Arrange
            var attr = new SecureValueAttribute("class", null);

            var data = new byte[] { 1, 2, 3, 4 };
            var result = new byte[] { 1, 2, 3, 4 };

            var encrMock = new Mock<IEncryptor>(MockBehavior.Strict);
            encrMock.SetupProperty(m => m.Authenticator);
            encrMock.Setup(m => m.Decrypt(data))
                .Returns(result);

            var provMock = new Mock<IEncryptionProvider>(MockBehavior.Strict);
            provMock.Setup(m => m.GetInstance("class"))
                .Returns(() => encrMock.Object);
            GlobalSettings.EncryptionProvider = provMock.Object;

            // Act
            var res = attr.Decrypt(data);

            // Assert
            Assert.AreSame(result, res);
            Assert.AreEqual(0, encrMock.Object.Authenticator.Length);
        }

        [TestMethod]
        public void Decrypt__Can_use_Authenticator()
        {
            // Arrange
            var attr = new SecureValueAttribute("class", nameof(ClassWithAuthenticator.AuthenticatorValue));

            attr.SetContext(new ClassWithAuthenticator { AuthenticatorValue = 100 });

            var data = new byte[] { 1, 2, 3, 4 };
            var result = new byte[] { 1, 2, 3, 4 };

            var encrMock = new Mock<IEncryptor>(MockBehavior.Strict);
            encrMock.SetupProperty(m => m.Authenticator);
            encrMock.Setup(m => m.Decrypt(data))
                .Returns(result);

            var provMock = new Mock<IEncryptionProvider>(MockBehavior.Strict);
            provMock.Setup(m => m.GetInstance("class"))
                .Returns(() => encrMock.Object);
            GlobalSettings.EncryptionProvider = provMock.Object;

            // Act
            var res = attr.Decrypt(data);

            // Assert
            Assert.AreSame(result, res);
            CollectionAssert.AreEqual(BitConverter.GetBytes(100L), encrMock.Object.Authenticator);
        }

        [TestMethod]
        public void Encrypt__Null__Empty()
        {
            // Arrange
            var attr = new SecureValueAttribute("class", null);

            // Act
            var res = attr.Encrypt(null);

            // Assert
            Assert.AreEqual(0, res.Length);
        }

        [TestMethod]
        public void Encrypt__Null_provider__Exception()
        {
            // Arrange
            var attr = new SecureValueAttribute("class", null);

            GlobalSettings.EncryptionProvider = null;

            // Act
            Assert.ThrowsException<NullReferenceException>(() =>
            {
                _ = attr.Encrypt(new byte[] { 1, 2, 3, 4 });
            });
        }

        [TestMethod]
        public void Encrypt__Provider_gives_null_encryptor__NOOP()
        {
            // Arrange
            var attr = new SecureValueAttribute("class", null);

            var provMock = new Mock<IEncryptionProvider>(MockBehavior.Strict);
            provMock.Setup(m => m.GetInstance("class"))
                .Returns(() => null!);
            GlobalSettings.EncryptionProvider = provMock.Object;

            var data = new byte[] { 1, 2, 3, 4 };

            // Act
            var res = attr.Encrypt(data);

            // Assert
            Assert.AreSame(data, res);
        }

        [TestMethod]
        public void Encrypt__Encrypts_by_encryptor()
        {
            // Arrange
            var attr = new SecureValueAttribute("class", null);

            var data = new byte[] { 1, 2, 3, 4 };
            var result = new byte[] { 1, 2, 3, 4 };

            var encrMock = new Mock<IEncryptor>(MockBehavior.Strict);
            encrMock.SetupProperty(m => m.Authenticator);
            encrMock.Setup(m => m.Encrypt(data))
                .Returns(result);

            var provMock = new Mock<IEncryptionProvider>(MockBehavior.Strict);
            provMock.Setup(m => m.GetInstance("class"))
                .Returns(() => encrMock.Object);
            GlobalSettings.EncryptionProvider = provMock.Object;

            // Act
            var res = attr.Encrypt(data);

            // Assert
            Assert.AreSame(result, res);
            Assert.AreEqual(0, encrMock.Object.Authenticator.Length);
        }

        [TestMethod]
        public void Encrypt__Can_use_Authenticator()
        {
            // Arrange
            var attr = new SecureValueAttribute("class", nameof(ClassWithAuthenticator.AuthenticatorValue));

            attr.SetContext(new ClassWithAuthenticator { AuthenticatorValue = 100 });

            var data = new byte[] { 1, 2, 3, 4 };
            var result = new byte[] { 1, 2, 3, 4 };

            var encrMock = new Mock<IEncryptor>(MockBehavior.Strict);
            encrMock.SetupProperty(m => m.Authenticator);
            encrMock.Setup(m => m.Encrypt(data))
                .Returns(result);

            var provMock = new Mock<IEncryptionProvider>(MockBehavior.Strict);
            provMock.Setup(m => m.GetInstance("class"))
                .Returns(() => encrMock.Object);
            GlobalSettings.EncryptionProvider = provMock.Object;

            // Act
            var res = attr.Encrypt(data);

            // Assert
            Assert.AreSame(result, res);
            CollectionAssert.AreEqual(BitConverter.GetBytes(100L), encrMock.Object.Authenticator);
        }
    }
}