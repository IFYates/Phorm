using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Data;

namespace IFY.Phorm.SqlClient.Tests
{
    [TestClass]
    public class TransactedSqlPhormRunnerTests
    {
        [TestMethod]
        public void Commit()
        {
            // Arrange
            var transMock = new Mock<IDbTransaction>(MockBehavior.Strict);
            transMock.Setup(m => m.Commit()).Verifiable();

            var runner = new TransactedSqlPhormRunner(null, null, transMock.Object);

            // Act
            runner.Commit();

            // Assert
            transMock.Verify();
        }

        [TestMethod]
        public void Rollback()
        {
            // Arrange
            var transMock = new Mock<IDbTransaction>(MockBehavior.Strict);
            transMock.Setup(m => m.Rollback()).Verifiable();

            var runner = new TransactedSqlPhormRunner(null, null, transMock.Object);

            // Act
            runner.Rollback();

            // Assert
            transMock.Verify();
        }

        [TestMethod]
        public void Dispose()
        {
            // Arrange
            var connMock = new Mock<IDbConnection>(MockBehavior.Strict);
            connMock.Setup(m => m.Dispose()).Verifiable();

            var transMock = new Mock<IDbTransaction>(MockBehavior.Strict);
            transMock.SetupGet(m => m.Connection)
                .Returns(connMock.Object);
            transMock.Setup(m => m.Dispose()).Verifiable();

            var runner = new TransactedSqlPhormRunner(null, null, transMock.Object);

            // Act
            runner.Dispose();

            // Assert
            transMock.Verify();
            connMock.Verify();
        }
    }
}