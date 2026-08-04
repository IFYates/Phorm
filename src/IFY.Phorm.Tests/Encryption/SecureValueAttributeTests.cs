using Moq;

namespace IFY.Phorm.Encryption.Tests;

[TestClass]
[DoNotParallelize]
public class SecureValueAttributeTests
{
    public class ClassWithAuthenticator
    {
        public long AuthenticatorValue { get; set; }
        public long AuthenticatorValue2 { get; set; }
    }

    [TestMethod]
    public void Decrypt__Null__Empty()
    {
        // Arrange
        var attr = new SecureValueAttribute("class", null);

        // Act
        var res = attr.Decrypt(null, null)!;

        // Assert
        Assert.IsEmpty(res);
    }

    [TestMethod]
    public void Decrypt__Null_provider__Exception()
    {
        // Arrange
        var attr = new SecureValueAttribute("class", null);

        GlobalSettings.EncryptionProvider = null;

        // Act
        Assert.ThrowsExactly<InvalidOperationException>
            (() => attr.Decrypt([1, 2, 3, 4], null));
    }

    [TestMethod]
    public void Decrypt__Provider_gives_null_encryptor__NOOP()
    {
        // Arrange
        var attr = new SecureValueAttribute("class", null);

        var provMock = new Mock<IEncryptionProvider>(MockBehavior.Strict);
        provMock.Setup(m => m.GetDecryptor("class", It.IsAny<byte[]>()))
            .Returns(() => null!);
        GlobalSettings.EncryptionProvider = provMock.Object;

        var data = new byte[] { 1, 2, 3, 4 };

        // Act
        var res = attr.Decrypt(data, null);

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
        provMock.Setup(m => m.GetDecryptor("class", data))
            .Returns(() => encrMock.Object);
        GlobalSettings.EncryptionProvider = provMock.Object;

        // Act
        var res = attr.Decrypt(data, null);

        // Assert
        Assert.AreSame(result, res);
        Assert.IsEmpty(encrMock.Object.Authenticator);
    }

    [TestMethod]
    public void Decrypt__Can_use_Authenticator()
    {
        // Arrange
        var attr = new SecureValueAttribute("class", nameof(ClassWithAuthenticator.AuthenticatorValue));

        var context = new ClassWithAuthenticator { AuthenticatorValue = 100 };

        var data = new byte[] { 1, 2, 3, 4 };
        var result = new byte[] { 1, 2, 3, 4 };

        var encrMock = new Mock<IEncryptor>(MockBehavior.Strict);
        encrMock.SetupProperty(m => m.Authenticator);
        encrMock.Setup(m => m.Decrypt(data))
            .Returns(result);

        var provMock = new Mock<IEncryptionProvider>(MockBehavior.Strict);
        provMock.Setup(m => m.GetDecryptor("class", data))
            .Returns(() => encrMock.Object);
        GlobalSettings.EncryptionProvider = provMock.Object;

        // Act
        var res = attr.Decrypt(data, context);

        // Assert
        Assert.AreSame(result, res);
        Assert.AreSequenceEqual(BitConverter.GetBytes(100L), encrMock.Object.Authenticator);
    }

    [TestMethod]
    public void Encrypt__Null__Empty()
    {
        // Arrange
        var attr = new SecureValueAttribute("class", null);

        // Act
        var res = attr.Encrypt(null, null);

        // Assert
        Assert.IsEmpty(res);
    }

    [TestMethod]
    public void Encrypt__Null_provider__Exception()
    {
        // Arrange
        var attr = new SecureValueAttribute("class", null);

        GlobalSettings.EncryptionProvider = null;

        // Act
        Assert.ThrowsExactly<InvalidOperationException>
            (() => attr.Encrypt(new byte[] { 1, 2, 3, 4 }, null));
    }

    [TestMethod]
    public void Encrypt__Provider_gives_null_encryptor__NOOP()
    {
        // Arrange
        var attr = new SecureValueAttribute("class", null);

        var provMock = new Mock<IEncryptionProvider>(MockBehavior.Strict);
        provMock.Setup(m => m.GetEncryptor("class"))
            .Returns(() => null!);
        GlobalSettings.EncryptionProvider = provMock.Object;

        var data = new byte[] { 1, 2, 3, 4 };

        // Act
        var res = attr.Encrypt(data, null);

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
        provMock.Setup(m => m.GetEncryptor("class"))
            .Returns(() => encrMock.Object);
        GlobalSettings.EncryptionProvider = provMock.Object;

        // Act
        var res = attr.Encrypt(data, null);

        // Assert
        Assert.AreSame(result, res);
        Assert.IsEmpty(encrMock.Object.Authenticator);
    }

    [TestMethod]
    public void Encrypt__Can_use_Authenticator()
    {
        // Arrange
        var attr = new SecureValueAttribute("class", nameof(ClassWithAuthenticator.AuthenticatorValue));

        var context = new ClassWithAuthenticator { AuthenticatorValue = 100 };

        var data = new byte[] { 1, 2, 3, 4 };
        var result = new byte[] { 1, 2, 3, 4 };

        var encrMock = new Mock<IEncryptor>(MockBehavior.Strict);
        encrMock.SetupProperty(m => m.Authenticator);
        encrMock.Setup(m => m.Encrypt(data))
            .Returns(result);

        var provMock = new Mock<IEncryptionProvider>(MockBehavior.Strict);
        provMock.Setup(m => m.GetEncryptor("class"))
            .Returns(() => encrMock.Object);
        GlobalSettings.EncryptionProvider = provMock.Object;

        // Act
        var res = attr.Encrypt(data, context);

        // Assert
        Assert.AreSame(result, res);
        Assert.AreSequenceEqual(BitConverter.GetBytes(100L), encrMock.Object.Authenticator);
    }

    // Breaks before v3.0.1
    [TestMethod]
    public void Encrypt__Can_use_multiple_Authenticators_in_the_same_object()
    {
        // Arrange
        var attr1 = new SecureValueAttribute("class", nameof(ClassWithAuthenticator.AuthenticatorValue));
        var attr2 = new SecureValueAttribute("class", nameof(ClassWithAuthenticator.AuthenticatorValue2));

        var context = new ClassWithAuthenticator { AuthenticatorValue = 100, AuthenticatorValue2 = 200 };

        var data = new byte[] { 1, 2, 3, 4 };
        var result = new byte[] { 1, 2, 3, 4 };

        var encrMock = new Mock<IEncryptor>(MockBehavior.Strict);
        encrMock.SetupProperty(m => m.Authenticator);
        encrMock.Setup(m => m.Encrypt(data))
            .Returns(() => encrMock.Object.Authenticator);

        var provMock = new Mock<IEncryptionProvider>(MockBehavior.Strict);
        provMock.Setup(m => m.GetEncryptor("class"))
            .Returns(() => encrMock.Object);
        GlobalSettings.EncryptionProvider = provMock.Object;

        // Act
        var res1 = attr1.Encrypt(data, context);
        var res2 = attr2.Encrypt(data, context);

        // Assert
        Assert.AreSequenceEqual(BitConverter.GetBytes(100L), res1);
        Assert.AreSequenceEqual(BitConverter.GetBytes(200L), res2);
    }
}