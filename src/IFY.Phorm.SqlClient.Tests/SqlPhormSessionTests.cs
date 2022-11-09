using IFY.Phorm.Connectivity;
using Microsoft.Data.SqlClient;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Data;
using System.Reflection;

namespace IFY.Phorm.SqlClient.Tests
{
    [TestClass]
    public class SqlPhormSessionTests
    {
        private const string DB_CONN = "Data Source=local";

        [TestMethod]
        public void SupportsTransactions__True()
        {
            // Arrange
            var obj = new SqlPhormSession(null!);

            // Assert
            Assert.IsTrue(obj.SupportsTransactions);
        }

        [TestMethod]
        public void IsInTransaction__False()
        {
            // Arrange
            var obj = new SqlPhormSession(null!);

            // Assert
            Assert.IsFalse(obj.IsInTransaction);
        }

        [TestMethod]
        public void BeginTransaction()
        {
            // Arrange
            var mocks = new MockRepository(MockBehavior.Strict);

            var tranMock = mocks.Create<IDbTransaction>();

            var connMock = mocks.Create<IPhormDbConnection>();
            connMock.SetupGet(m => m.DefaultSchema)
                .Returns("dbo");
            connMock.Setup(m => m.Open());
            connMock.Setup(m => m.BeginTransaction())
                .Returns(tranMock.Object).Verifiable();

            var connName = Guid.NewGuid().ToString();

            var sess = new SqlPhormSession(DB_CONN, connName)
            {
                _connectionBuilder = (cs, cn) =>
                {
                    connMock.SetupGet(m => m.ConnectionString).Returns(cs);
                    connMock.SetupGet(m => m.ConnectionName).Returns(cn);
                    return connMock.Object;
                }
            };

            // Act
            var res = (TransactedSqlPhormSession)sess.BeginTransaction();

            // Assert
            mocks.Verify();
            Assert.AreSame(tranMock.Object, getField(res, "_transaction"));
            Assert.AreEqual(DB_CONN + ";Application Name=" + connName, res.GetConnection().ConnectionString);
            Assert.AreEqual(connName, res.ConnectionName);
        }

        [TestMethod]
        public void GetConnection()
        {
            // Arrange
            var connMock = new Mock<IPhormDbConnection>();
            connMock.SetupGet(m => m.DefaultSchema).Returns("dbo");

            var sess = new SqlPhormSession(DB_CONN, null!)
            {
                _connectionBuilder = (cs, cn) => connMock.Object
            };

            // Act
            var res = sess.GetConnection();

            // Assert
            Assert.AreSame(connMock.Object, res);
        }

        [TestMethod]
        public void GetConnection__Fires_Connected_event()
        {
            // Arrange
            var connMock = new Mock<IPhormDbConnection>();
            connMock.SetupGet(m => m.DefaultSchema).Returns("dbo");

            var sess = new SqlPhormSession(DB_CONN, null!)
            {
                _connectionBuilder = (cs, cn) => connMock.Object
            };

            object? eventSender = null, eventArgs = null;
            sess.Connected += (s, e) =>
            {
                eventSender = s;
                eventArgs = e;
            };

            // Act
            _ = sess.GetConnection();

            // Assert
            Assert.AreSame(sess, eventSender);
            Assert.AreSame(connMock.Object, eventArgs);
        }

        [TestMethod]
        public void GetConnection__Connected_event_ignores_handler_exception()
        {
            // Arrange
            var connMock = new Mock<IPhormDbConnection>();
            connMock.SetupGet(m => m.DefaultSchema).Returns("dbo");

            var sess = new SqlPhormSession(DB_CONN, null!)
            {
                _connectionBuilder = (cs, cn) => connMock.Object
            };

            sess.Connected += (s, e) =>
            {
                throw new NotImplementedException();
            };

            // Act
            var res = sess.GetConnection();

            // Assert
            Assert.AreSame(connMock.Object, res);
        }

        [TestMethod]
        public void SetConnectionName__Returns_new_session_with_updated_connection_name()
        {
            // Arrange
            var sess1 = new SqlPhormSession(null!, "name1");

            // Act
            var sess2 = (SqlPhormSession)sess1.SetConnectionName("name2");

            // Assert
            Assert.AreNotSame(sess2, sess1);
            Assert.AreEqual("name1", sess1.ConnectionName);
            Assert.AreEqual("name2", sess2.ConnectionName);
        }

        static object? getField<T>(T inst, string fieldName)
        {
            return typeof(T).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(inst);
        }

        [TestMethod]
        public void StartConsoleCapture__Not_SqlConnection__NullConsoleMessageCapture()
        {
            // Arrange
            var conn = new Mock<IDbConnection>(MockBehavior.Strict).Object;

            var cmdMock = new Mock<IAsyncDbCommand>(MockBehavior.Strict);
            cmdMock.SetupGet(m => m.Connection)
                .Returns(conn);

            var obj = new SqlPhormSession(null!);

            // Act
            var res = obj.StartConsoleCapture(Guid.Empty, cmdMock.Object);

            // Assert
            Assert.AreSame(AbstractPhormSession.NullConsoleMessageCapture.Instance, res);
            Assert.IsFalse(res.HasError);
        }

        [TestMethod]
        public void StartConsoleCapture__SqlConnection__New_SqlConsoleMessageCapture()
        {
            // Arrange
            var conn = new SqlConnection();

            var cmdMock = new Mock<IAsyncDbCommand>(MockBehavior.Strict);
            cmdMock.SetupGet(m => m.Connection)
                .Returns(conn);

            var obj = new SqlPhormSession(null!);

            // Act
            var res = (SqlConsoleMessageCapture)obj.StartConsoleCapture(Guid.Empty, cmdMock.Object);

            // Assert
            Assert.IsFalse(res.HasError);
        }
    }
}