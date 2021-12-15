using IFY.Phorm.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Data;

namespace IFY.Phorm.Tests
{
    [TestClass]
    public class AbstractPhormSessionTests
    {
        public interface ITestContract : IPhormContract
        {
        }

        [TestMethod]
        public void From__No_typearg()
        {
            // Arrange
            var phorm = new TestPhormSession();

            // Act
            var runner = phorm.From("objectName");

            // Assert
            Assert.IsInstanceOfType(runner, typeof(PhormContractRunner<IPhormContract>));
        }

        [TestMethod]
        public void From_With_typearg()
        {
            // Arrange
            var phorm = new TestPhormSession();


            // Act
            var runner = phorm.From<ITestContract>();

            // Assert
            Assert.IsInstanceOfType(runner, typeof(PhormContractRunner<ITestContract>));
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void Call__By_object(bool byAsync)
        {
            // Arange
            var phorm = new TestPhormSession();

            // Act
            int res;
            if (byAsync)
            {
                res = phorm.CallAsync("TestContract").Result;
            }
            else
            {
                res = phorm.Call("TestContract");
            }

            // Assert
            Assert.AreEqual(1, res);
            Assert.AreEqual("[dbo].[TestContract]", phorm.Commands[0].CommandText);
            Assert.AreEqual(CommandType.StoredProcedure, phorm.Commands[0].CommandType);
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void Call__By_contract_and_object(bool byAsync)
        {
            // Arange
            var phorm = new TestPhormSession();

            // Act
            int res;
            if (byAsync)
            {
                res = phorm.CallAsync<ITestContract>((object?)null).Result;
            }
            else
            {
                res = phorm.Call<ITestContract>((object?)null);
            }

            // Assert
            Assert.AreEqual(1, res);
            Assert.AreEqual("[dbo].[TestContract]", phorm.Commands[0].CommandText);
            Assert.AreEqual(CommandType.StoredProcedure, phorm.Commands[0].CommandType);
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void Call__By_contract(bool byAsync)
        {
            // Arange
            var phorm = new TestPhormSession();

            var objMock = new Mock<ITestContract>();

            // Act
            int res;
            if (byAsync)
            {
                res = phorm.CallAsync(objMock.Object).Result;
            }
            else
            {
                res = phorm.Call(objMock.Object);
            }

            // Assert
            Assert.AreEqual(1, res);
            Assert.AreEqual("[dbo].[TestContract]", phorm.Commands[0].CommandText);
            Assert.AreEqual(CommandType.StoredProcedure, phorm.Commands[0].CommandType);
        }
    }
}