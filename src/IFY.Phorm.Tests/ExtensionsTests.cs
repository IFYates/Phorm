using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace IFY.Phorm.Tests
{
    [TestClass]
    public class ExtensionsTests
    {
        [TestMethod]
        public void GetBytes__Null__Empty()
        {
            // Act
            var res = ((object?)null).GetBytes();

            // Assert
            Assert.AreEqual(0, res.Length);
        }

        [TestMethod]
        [DataRow(12.34, typeof(decimal))]
        [DataRow(new byte[] { 1, 2, 3, 4 }, typeof(byte[]))]
        [DataRow(123, typeof(byte))]
        [DataRow(123, typeof(char))]
        [DataRow(12.34, typeof(double))]
        [DataRow(12.34, typeof(float))]
        [DataRow(1234, typeof(int))]
        [DataRow(1234, typeof(long))]
        [DataRow(1234, typeof(short))]
        [DataRow("1234", typeof(string))]
        public void GetBytes__Supports_basic_types(object data, Type type)
        {
            // Arrange
            if (data.GetType() != type)
            {
                data = Convert.ChangeType(data, type);
            }

            // Act
            var res = data.GetBytes();

            // Assert
            Assert.AreNotEqual(0, res.Length);
        }

        [TestMethod]
        public void GetBytes__Supports_Guid()
        {
            // Arrange
            var data = Guid.NewGuid();

            // Act
            var res = data.GetBytes();

            // Assert
            Assert.AreNotEqual(0, res.Length);
        }
    }
}