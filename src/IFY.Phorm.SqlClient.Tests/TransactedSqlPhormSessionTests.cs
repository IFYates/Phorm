using IFY.Phorm.Connectivity;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Data;

namespace IFY.Phorm.SqlClient.Tests
{
    [TestClass]
    public class TransactedSqlPhormSessionTests
    {
        [TestInitialize]
        public void Init()
        {
            AbstractPhormSession.ResetConnectionPool();
        }

        [TestMethod]
        public void IsInTransaction__True()
        {
            // Arrange
            var connMock = new Mock<IPhormDbConnection>();

            var sess = new TransactedSqlPhormSession(connMock.Object, null!);

            // Assert
            Assert.IsTrue(sess.IsInTransaction);
        }

        [TestMethod]
        public void GetConnection()
        {
            // Arrange
            var connMock = new Mock<IPhormDbConnection>();

            var sess = new TransactedSqlPhormSession(connMock.Object, null!);

            // Act
            var res = sess.GetConnection();

            // Assert
            Assert.AreSame(connMock.Object, res);
        }

        [TestMethod]
        public void Commit()
        {
            // Arrange
            var connMock = new Mock<IPhormDbConnection>();

            var transMock = new Mock<IDbTransaction>(MockBehavior.Strict);
            transMock.Setup(m => m.Commit()).Verifiable();

            var sess = new TransactedSqlPhormSession(connMock.Object, transMock.Object);

            // Act
            sess.Commit();

            // Assert
            transMock.Verify();
            Assert.IsTrue(sess.IsInTransaction);
        }

        [TestMethod]
        public void Rollback()
        {
            // Arrange
            var connMock = new Mock<IPhormDbConnection>();

            var transMock = new Mock<IDbTransaction>(MockBehavior.Strict);
            transMock.Setup(m => m.Rollback()).Verifiable();

            var sess = new TransactedSqlPhormSession(connMock.Object, transMock.Object);

            // Act
            sess.Rollback();

            // Assert
            transMock.Verify();
        }

        [TestMethod]
        public void Dispose()
        {
            // Arrange
            var connMock = new Mock<IPhormDbConnection>(MockBehavior.Strict);
            connMock.SetupAllProperties();
            connMock.Setup(m => m.Dispose()).Verifiable();

            var transMock = new Mock<IDbTransaction>(MockBehavior.Strict);
            transMock.Setup(m => m.Dispose()).Verifiable();

            var sess = new TransactedSqlPhormSession(connMock.Object, transMock.Object);

            // Act
            sess.Dispose();

            // Assert
            transMock.Verify();
            connMock.Verify();
        }
    }
}