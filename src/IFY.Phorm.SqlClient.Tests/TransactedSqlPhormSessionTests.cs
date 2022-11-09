using IFY.Phorm.Connectivity;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Data;

namespace IFY.Phorm.SqlClient.Tests
{
    [TestClass]
    public class TransactedSqlPhormSessionTests
    {
        [TestMethod]
        public void Commit()
        {
            // Arrange
            var transMock = new Mock<IDbTransaction>(MockBehavior.Strict);
            transMock.Setup(m => m.Commit()).Verifiable();

            var connMock = new Mock<IPhormDbConnection>();

            var runner = new TransactedSqlPhormSession(connMock.Object, transMock.Object);

            // Act
            runner.Commit();

            // Assert
            transMock.Verify();
            Assert.IsTrue(runner.IsInTransaction);
        }

        [TestMethod]
        public void Rollback()
        {
            // Arrange
            var transMock = new Mock<IDbTransaction>(MockBehavior.Strict);
            transMock.Setup(m => m.Rollback()).Verifiable();

            var connMock = new Mock<IPhormDbConnection>();

            var runner = new TransactedSqlPhormSession(connMock.Object, transMock.Object);

            // Act
            runner.Rollback();

            // Assert
            transMock.Verify();
        }

        [TestMethod]
        public void Dispose()
        {
            // Arrange
            var transMock = new Mock<IDbTransaction>(MockBehavior.Strict);
            transMock.Setup(m => m.Dispose()).Verifiable();

            var connMock = new Mock<IPhormDbConnection>(MockBehavior.Strict);
            connMock.SetupAllProperties();
            connMock.Setup(m => m.Dispose()).Verifiable();

            var runner = new TransactedSqlPhormSession(connMock.Object, transMock.Object);

            // Act
            runner.Dispose();

            // Assert
            transMock.Verify();
            connMock.Verify();
        }
    }
}