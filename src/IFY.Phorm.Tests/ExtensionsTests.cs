using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections;

namespace IFY.Phorm.Tests
{
    [TestClass]
    public class ExtensionsTests
    {
        #region GetBytes

        [TestMethod]
        public void GetBytes__Null__Empty()
        {
            // Act
            var res = ((object?)null).GetBytes();

            // Assert
            Assert.AreEqual(0, res.Length);
        }

        [TestMethod]
        [DataRow(new byte[] { 210, 4, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2, 0 }, 12.34, typeof(decimal))]
        [DataRow(new byte[] { 1, 2, 3, 4 }, new byte[] { 1, 2, 3, 4 }, typeof(byte[]))]
        [DataRow(new byte[] { 123 }, 123, typeof(byte))]
        [DataRow(new byte[] { 123, 0 }, 123, typeof(char))]
        [DataRow(new byte[] { 174, 71, 225, 122, 20, 174, 40, 64 }, 12.34, typeof(double))]
        [DataRow(new byte[] { 164, 112, 69, 65 }, 12.34, typeof(float))]
        [DataRow(new byte[] { 210, 4, 0, 0 }, 1234, typeof(int))]
        [DataRow(new byte[] { 210, 4, 0, 0, 0, 0, 0, 0 }, 1234, typeof(long))]
        [DataRow(new byte[] { 210, 4 }, 1234, typeof(short))]
        [DataRow(new byte[] { 49, 50, 51, 52 }, "1234", typeof(string))]
        public void GetBytes__Supports_basic_types(byte[] exp, object data, Type type)
        {
            // Arrange
            if (data.GetType() != type)
            {
                data = Convert.ChangeType(data, type);
            }

            // Act
            var res = data.GetBytes();

            // Assert
            CollectionAssert.AreEqual(exp, res);
        }

        [TestMethod]
        public void GetBytes__Supports_Guid()
        {
            // Arrange
            var exp = new byte[]
            {
                131, 190, 113, 220, 122, 94, 160, 76, 178, 33, 70, 205, 145, 251, 255, 152
            };

            var data = Guid.Parse("dc71be83-5e7a-4ca0-b221-46cd91fbff98");

            // Act
            var res = data.GetBytes();

            // Assert
            CollectionAssert.AreEqual(exp, res);
        }

        [TestMethod]
        public void GetBytes__Unsupported_type__Exception()
        {
            // Act
            Assert.ThrowsException<InvalidCastException>(() =>
            {
                _ = new ExtensionsTests().GetBytes();
            });
        }

        #endregion GetBytes

        #region FromBytes

        [TestMethod]
        public void FromBytes__Null__Null()
        {
            // Act
            var res = Extensions.FromBytes<string>(null);

            // Assert
            Assert.IsNull(res);
        }

        [TestMethod]
        [DataRow(new byte[] { 210, 4, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2, 0 }, 12.34, typeof(decimal))]
        [DataRow(new byte[] { 1, 2, 3, 4 }, new byte[] { 1, 2, 3, 4 }, typeof(byte[]))]
        [DataRow(new byte[] { 123 }, 123, typeof(byte))]
        [DataRow(new byte[] { 123, 0 }, 123, typeof(char))]
        [DataRow(new byte[] { 174, 71, 225, 122, 20, 174, 40, 64 }, 12.34, typeof(double))]
        [DataRow(new byte[] { 164, 112, 69, 65 }, 12.34, typeof(float))]
        [DataRow(new byte[] { 210, 4, 0, 0 }, 1234, typeof(int))]
        [DataRow(new byte[] { 210, 4, 0, 0, 0, 0, 0, 0 }, 1234, typeof(long))]
        [DataRow(new byte[] { 210, 4 }, 1234, typeof(short))]
        [DataRow(new byte[] { 49, 50, 51, 52 }, "1234", typeof(string))]
        public void FromBytes__Supports_basic_types(byte[] data, object exp, Type type)
        {
            // Act
            if (exp.GetType() != type)
            {
                exp = Convert.ChangeType(exp, type);
            }

            var res = Extensions.FromBytes(data, type);

            // Assert
            if (type.IsArray)
            {
                CollectionAssert.AreEqual((ICollection)exp, (ICollection?)res);
            }
            else
            {
                Assert.AreEqual(exp, res);
            }
        }

        [TestMethod]
        public void FromBytes__Supports_Guid()
        {
            // Arrange
            var data = new byte[]
            {
                131, 190, 113, 220, 122, 94, 160, 76, 178, 33, 70, 205, 145, 251, 255, 152
            };

            // Act
            var res = data.FromBytes<Guid>();

            // Assert
            Assert.AreEqual("dc71be83-5e7a-4ca0-b221-46cd91fbff98", res.ToString());
        }

        [TestMethod]
        public void FromBytes__Unsupported_type__Exception()
        {
            // Act
            Assert.ThrowsException<InvalidCastException>(() =>
            {
                _ = Extensions.FromBytes(Array.Empty<byte>(), typeof(ExtensionsTests));
            });
        }

        #endregion FromBytes
    }
}