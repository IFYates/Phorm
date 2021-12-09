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
        public void BeginTransaction()
        {
            // Assert
            var dbMock = new Mock<IPhormDbConnection>(MockBehavior.Strict);
            dbMock.Setup(m => m.BeginTransaction())
                .Returns(() => null).Verifiable();

            var db = new PhormDbConnection("", dbMock.Object);

            // Act
            db.BeginTransaction();

            // Assert
            dbMock.Verify();
        }

        [TestMethod]
        public void BeginTransaction__With_IsolationLevel()
        {
            // Assert
            var dbMock = new Mock<IPhormDbConnection>(MockBehavior.Strict);
            dbMock.Setup(m => m.BeginTransaction(It.IsAny<IsolationLevel>()))
                .Returns(() => null).Verifiable();

            var db = new PhormDbConnection("", dbMock.Object);

            // Act
            db.BeginTransaction(IsolationLevel.Chaos);

            // Assert
            dbMock.Verify();
        }

        [TestMethod]
        public void ChangeDatabase()
        {
            // Assert
            var dbMock = new Mock<IPhormDbConnection>(MockBehavior.Strict);
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
            // Assert
            var dbMock = new Mock<IPhormDbConnection>(MockBehavior.Strict);
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
            // Assert
            var dbMock = new Mock<IPhormDbConnection>(MockBehavior.Strict);
            dbMock.Setup(m => m.Close())
                .Verifiable();

            var db = new PhormDbConnection("", dbMock.Object);

            // Act
            db.Close();

            // Assert
            dbMock.Verify();
        }

        [TestMethod]
        public void CreateCommand__Wraps_command_as_IAsyncDbCommand()
        {
            // Assert
            var cmdText = Guid.NewGuid().ToString();
            var cmd = new TestDbCommand
            {
                CommandText = cmdText
            };

            var dbMock = new Mock<IDbConnection>(MockBehavior.Strict);
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
        public void Dispose()
        {
            // Assert
            var dbMock = new Mock<IPhormDbConnection>(MockBehavior.Strict);
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