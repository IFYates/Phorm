using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace IFY.Phorm.Transformation.Tests
{
    [TestClass]
    public class JsonValueAttributeTests
    {
        [TestMethod]
        public void FromDatasource__Null_Null()
        {
            // Arrange
            var attr = new JsonValueAttribute();

            // Act
            var res = attr.FromDatasource(typeof(int), null);

            // Assert
            Assert.IsNull(res);
        }

        [TestMethod]
        public void FromDatasource__Data_is_JSON_of_type()
        {
            // Arrange
            var attr = new JsonValueAttribute();

            // Act
            var res = attr.FromDatasource(typeof(int), "1234");

            // Assert
            Assert.AreEqual(1234, res);
        }

        [TestMethod]
        public void FromDatasource__Data_is_not_JSON_of_type__Exception()
        {
            // Arrange
            var attr = new JsonValueAttribute();

            // Act
            Assert.ThrowsException<JsonException>(() =>
            {
                attr.FromDatasource(typeof(int), "invalid");
            });
        }

        [TestMethod]
        public void ToDatasource__Null__Null()
        {
            // Arrange
            var attr = new JsonValueAttribute();

            // Act
            var res = attr.ToDatasource(null);

            // Assert
            Assert.IsNull(res);
        }

        [TestMethod]
        public void ToDatasource__Returns_JSON()
        {
            // Arrange
            var attr = new JsonValueAttribute();

            // Act
            var res = attr.ToDatasource("value");

            // Assert
            Assert.AreEqual("\"value\"", res);
        }
    }
}