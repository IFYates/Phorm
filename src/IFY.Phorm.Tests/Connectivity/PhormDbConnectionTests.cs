using IFY.Phorm.Tests;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Data;
using System.Data.Common;

namespace IFY.Phorm.Connectivity.Tests
{
    [TestClass]
    public class PhormDbConnectionTests
    {
        [TestMethod]
        public void Properties()
        {
            // Arrange
            var dbMock = new Mock<IDbConnection>(MockBehavior.Strict);
            dbMock.SetupProperty(m => m.ConnectionString);
            dbMock.SetupGet(m => m.ConnectionTimeout).Returns(1234);
            dbMock.SetupGet(m => m.Database).Returns("databaseName");
            dbMock.SetupGet(m => m.State).Returns(ConnectionState.Connecting);

            var db = new PhormDbConnection("contextName", dbMock.Object)
            {
                DefaultSchema = "schema",
                ConnectionString = "connString"
            };

            // Assert
            Assert.AreSame(dbMock.Object, db.DbConnection);
            Assert.AreEqual("contextName", db.ConnectionName);
            Assert.AreEqual("schema", db.DefaultSchema);
            Assert.AreEqual("connString", db.ConnectionString);
            Assert.AreEqual("connString", dbMock.Object.ConnectionString);
            Assert.AreEqual(1234, db.ConnectionTimeout);
            Assert.AreEqual("databaseName", db.Database);
            Assert.AreEqual(ConnectionState.Connecting, db.State);
        }

        [TestMethod]
        public void BeginTransaction()
        {
            // Arrange
            var dbMock = new Mock<IDbConnection>(MockBehavior.Strict);
            dbMock.Setup(m => m.BeginTransaction())
                .Returns(() => null!).Verifiable();

            var db = new PhormDbConnection("", dbMock.Object);

            // Act
            db.BeginTransaction();

            // Assert
            dbMock.Verify();
        }

        [TestMethod]
        public void BeginTransaction__With_IsolationLevel()
        {
            // Arrange
            var dbMock = new Mock<IDbConnection>(MockBehavior.Strict);
            dbMock.Setup(m => m.BeginTransaction(It.IsAny<IsolationLevel>()))
                .Returns(() => null!).Verifiable();

            var db = new PhormDbConnection("", dbMock.Object);

            // Act
            db.BeginTransaction(IsolationLevel.Chaos);

            // Assert
            dbMock.Verify();
        }

        [TestMethod]
        public void ChangeDatabase()
        {
            // Arrange
            var dbMock = new Mock<IDbConnection>(MockBehavior.Strict);
            dbMock.Setup(m => m.ChangeDatabase(It.IsAny<string>()))
                .Verifiable();

            var db = new PhormDbConnection("", dbMock.Object);

            // Act
            db.ChangeDatabase(string.Empty);

            // Assert
            dbMock.Verify();
        }

        [TestMethod]
        public void Open()
        {
            // Arrange
            var dbMock = new Mock<IDbConnection>(MockBehavior.Strict);
            dbMock.Setup(m => m.Open())
                .Verifiable();

            var db = new PhormDbConnection("", dbMock.Object);

            // Act
            db.Open();

            // Assert
            dbMock.Verify();
        }

        [TestMethod]
        public void Close()
        {
            // Arrange
            var dbMock = new Mock<IDbConnection>(MockBehavior.Strict);
            dbMock.Setup(m => m.Close())
                .Verifiable();

            var db = new PhormDbConnection("", dbMock.Object);

            // Act
            db.Close();

            // Assert
            dbMock.Verify();
        }

        [TestMethod]
        public void CreateCommand__Connection_open__Wraps_command_as_IAsyncDbCommand()
        {
            // Arrange
            var cmdText = Guid.NewGuid().ToString();
            var cmd = new TestDbCommand
            {
                CommandText = cmdText
            };

            var dbMock = new Mock<IDbConnection>(MockBehavior.Strict);
            dbMock.SetupGet(m => m.State)
                .Returns(ConnectionState.Open);
            dbMock.Setup(m => m.CreateCommand())
                .Returns(cmd).Verifiable();

            var db = new PhormDbConnection("", dbMock.Object);

            // Act
            var res = db.CreateCommand();

            // Assert
            Assert.AreEqual(cmdText, res.CommandText);
            dbMock.Verify();
        }

        [TestMethod]
        public void CreateCommand__Connection_closed__Opens_connection_and_wraps_command_as_IAsyncDbCommand()
        {
            // Arrange
            var cmdText = Guid.NewGuid().ToString();
            var cmd = new TestDbCommand
            {
                CommandText = cmdText
            };

            var dbMock = new Mock<IDbConnection>(MockBehavior.Strict);
            dbMock.SetupGet(m => m.State)
                .Returns(ConnectionState.Closed);
            dbMock.Setup(m => m.Open())
                .Verifiable();
            dbMock.Setup(m => m.CreateCommand())
                .Returns(cmd).Verifiable();

            var db = new PhormDbConnection("", dbMock.Object);

            // Act
            var res = db.CreateCommand();

            // Assert
            Assert.AreEqual(cmdText, res.CommandText);
            dbMock.Verify();
        }

        [TestMethod]
        public void CreateCommand__IDbConnection__Connection_open__Wraps_command_as_IAsyncDbCommand()
        {
            // Arrange
            var cmdMock = new Mock<IDbCommand>(MockBehavior.Strict);

            var dbMock = new Mock<IDbConnection>(MockBehavior.Strict);
            dbMock.SetupGet(m => m.State)
                .Returns(ConnectionState.Open);
            dbMock.Setup(m => m.CreateCommand())
                .Returns(cmdMock.Object).Verifiable();

            var db = new PhormDbConnection("", dbMock.Object);

            // Act
            var res = ((IDbConnection)db).CreateCommand();

            // Assert
            Assert.AreSame(cmdMock.Object, res);
            dbMock.Verify();
        }

        [TestMethod]
        public void CreateCommand__IDbConnection__Connection_closed__Opens_connection_and_wraps_command_as_IAsyncDbCommand()
        {
            // Arrange
            var cmdMock = new Mock<IDbCommand>(MockBehavior.Strict);

            var dbMock = new Mock<IDbConnection>(MockBehavior.Strict);
            dbMock.SetupGet(m => m.State)
                .Returns(ConnectionState.Closed);
            dbMock.Setup(m => m.Open())
                .Verifiable();
            dbMock.Setup(m => m.CreateCommand())
                .Returns(cmdMock.Object).Verifiable();

            var db = new PhormDbConnection("", dbMock.Object);

            // Act
            var res = ((IDbConnection)db).CreateCommand();

            // Assert
            Assert.AreSame(cmdMock.Object, res);
            dbMock.Verify();
        }

        [TestMethod]
        public void Dispose()
        {
            // Arrange
            var dbMock = new Mock<IDbConnection>(MockBehavior.Strict);
            dbMock.Setup(m => m.Dispose())
                .Verifiable();

            var db = new PhormDbConnection("", dbMock.Object);

            // Act
            db.Dispose();

            // Assert
            dbMock.Verify();
        }
    }
}