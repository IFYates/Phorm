using Microsoft.VisualStudio.TestTools.UnitTesting;
using IFY.Phorm.Encryption;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Moq;

namespace IFY.Phorm.Encryption.Tests
{
    [TestClass()]
    public class SecureValueAttributeTests
    {
        [TestMethod()]
        public void SecureValueAttributeTest()
        {
            Assert.Fail();
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
        public void Decrypt__Provider_gives_null_encrytor__NOOP()
        {
            // Arrange
            var attr = new SecureValueAttribute("class", null);

            var provMock = new Mock<IEncryptionProvider>(MockBehavior.Strict);
            provMock.Setup(m => m.GetInstance("class"))
                .Returns(() => null);
            GlobalSettings.EncryptionProvider = provMock.Object;

            var data = new byte[] { 1, 2, 3, 4 };

            // Act
            var res = attr.Decrypt(data);

            // Assert
            Assert.AreSame(data, res);
        }

        [TestMethod()]
        public void EncryptTest()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void SetContextTest()
        {
            Assert.Fail();
        }
    }
}