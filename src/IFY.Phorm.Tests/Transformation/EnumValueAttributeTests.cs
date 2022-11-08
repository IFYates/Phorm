using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Runtime.Serialization;

namespace IFY.Phorm.Transformation.Tests
{
    [TestClass]
    public class EnumValueAttributeTests
    {
        public enum TestEnum
        {
            Value1 = 1,
            Value5 = 5,
            [EnumMember(Value = "Value10")]
            ValueX = 10,
        }

        [TestMethod]
        public void FromDatasource__Null_nullable__Null()
        {
            // Arrange
            var attr = new EnumValueAttribute();

            // Act
            var res = attr.FromDatasource(typeof(TestEnum?), null, null);

            // Assert
            Assert.IsNull(res);
        }

        [TestMethod]
        public void FromDatasource__Null_not_nullable__Exception()
        {
            // Arrange
            var attr = new EnumValueAttribute();

            // Act
            Assert.ThrowsException<ArgumentNullException>(() =>
            {
                attr.FromDatasource(typeof(TestEnum), null, null);
            });
        }

        [TestMethod]
        public void FromDatasource__Already_enum__Noop()
        {
            // Arrange
            var attr = new EnumValueAttribute();

            object value = TestEnum.Value5;

            // Act
            var res = attr.FromDatasource(typeof(TestEnum?), value, null);

            // Assert
            Assert.AreSame(value, res);
        }

        [TestMethod]
        [DataRow("ValueX", TestEnum.ValueX)]
        [DataRow(10, TestEnum.ValueX)]
        [DataRow("10", TestEnum.ValueX)]
        [DataRow("Value10", TestEnum.ValueX)]
        public void FromDatasource__String_or_int_converted(object input, TestEnum exp)
        {
            // Arrange
            var attr = new EnumValueAttribute();

            // Act
            var res = attr.FromDatasource(typeof(TestEnum?), input, null);

            // Assert
            Assert.AreEqual(exp, res);
        }

        [TestMethod]
        [DataRow(0)]
        [DataRow(-10)]
        [DataRow(1000)]
        public void FromDatasource__Invalid_int__Returns(object input)
        {
            // Arrange
            var attr = new EnumValueAttribute();

            // Act
            var res = (TestEnum?)attr.FromDatasource(typeof(TestEnum), input, null);

            // Assert
            Assert.AreEqual(input, (int?)res);
            Assert.AreEqual(input.ToString(), res.ToString());
        }

        [TestMethod]
        [DataRow("Value99", DisplayName = "Unmatched string")]
        public void FromDatasource__Invalid_input__Exception(object input)
        {
            // Arrange
            var attr = new EnumValueAttribute();

            // Act
            Assert.ThrowsException<ArgumentException>(() =>
            {
                attr.FromDatasource(typeof(TestEnum), input, null);
            });
        }

        [TestMethod]
        public void ToDatasource__Null_Null()
        {
            // Arrange
            var attr = new EnumValueAttribute();

            // Act
            var res = attr.ToDatasource(null, null);

            // Assert
            Assert.IsNull(res);
        }

        [TestMethod]
        public void ToDatasource__Not_enum__Exception()
        {
            // Arrange
            var attr = new EnumValueAttribute();

            // Act
            Assert.ThrowsException<InvalidCastException>(() =>
            {
                attr.ToDatasource("str", null);
            });
        }

        [TestMethod]
        [DataRow(TestEnum.Value5, false, 5)]
        [DataRow(TestEnum.Value5, true, "Value5")]
        [DataRow(TestEnum.ValueX, true, "Value10")]
        public void ToDatasource__Converts_to_int_or_string(TestEnum input, bool asString, object exp)
        {
            // Arrange
            var attr = new EnumValueAttribute
            {
                SendAsString = asString
            };

            // Act
            var res = attr.ToDatasource(input, null);

            // Assert
            Assert.AreEqual(exp, res);
        }
    }
}