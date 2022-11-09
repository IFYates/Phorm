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
        private const string CONN_STR = "Data Source=local";

        [TestInitialize]
        public void Init()
        {
            AbstractPhormSession.ResetConnectionPool();
        }

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
            connMock.Setup(m => m.Dispose());
            connMock.SetupProperty(m => m.DefaultSchema).Object.DefaultSchema = "dbo";
            connMock.Setup(m => m.Open());
            connMock.Setup(m => m.BeginTransaction())
                .Returns(tranMock.Object).Verifiable();

            var connName = Guid.NewGuid().ToString();

            var sess = new SqlPhormSession(CONN_STR, connName)
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
            Assert.AreEqual(CONN_STR + ";Application Name=" + connName, res.GetConnection().ConnectionString);
            Assert.AreEqual(connName, res.ConnectionName);
        }

        [TestMethod]
        public void GetConnection()
        {
            // Arrange
            var connMock = new Mock<IPhormDbConnection>();
            connMock.Setup(m => m.Dispose());
            connMock.SetupProperty(m => m.DefaultSchema).Object.DefaultSchema = "dbo";

            var sess = new SqlPhormSession(CONN_STR, null!)
            {
                _connectionBuilder = (cs, cn) => connMock.Object
            };

            // Act
            var res = sess.GetConnection();

            // Assert
            Assert.AreSame(connMock.Object, res);
        }

        [TestMethod]
        public void GetConnection__First_connection__Gets_new_instance()
        {
            // Arrange
            var connMock = new Mock<IPhormDbConnection>(MockBehavior.Strict);
            connMock.Setup(m => m.Dispose());
            connMock.SetupProperty(m => m.DefaultSchema).Object.DefaultSchema = "dbo";

            var connName = Guid.NewGuid().ToString();

            string? connectionStringUsed = null;
            string? connectionNameUsed = null;
            var sess = new SqlPhormSession(CONN_STR, connName)
            {
                _connectionBuilder = (connectionString, connectionName) =>
                {
                    connectionStringUsed = connectionString;
                    connectionNameUsed = connectionName;
                    return connMock.Object;
                }
            };

            // Act
            var res = sess.GetConnection();

            // Assert
            Assert.AreEqual(CONN_STR + ";Application Name=" + connName, connectionStringUsed);
            Assert.AreEqual(connName, connectionNameUsed);
            Assert.AreSame(connMock.Object, res);
        }

        [TestMethod]
        public void GetConnection__Repeat_connection__Gets_same_open_instance()
        {
            // Arrange
            static Mock<IPhormDbConnection> getConnMock()
            {
                var connMock = new Mock<IPhormDbConnection>(MockBehavior.Strict);
                connMock.Setup(m => m.Dispose());
                connMock.SetupProperty(m => m.DefaultSchema).Object.DefaultSchema = "dbo";
                connMock.SetupGet(m => m.State)
                    .Returns(ConnectionState.Open);
                return connMock;
            }

            var sess = new SqlPhormSession(CONN_STR)
            {
                _connectionBuilder = (_, __) => getConnMock().Object
            };

            // Act
            var res1 = sess.GetConnection();
            var res2 = sess.GetConnection();

            // Assert
            Assert.AreSame(res1, res2);
        }

        [TestMethod]
        public void GetConnection__Multiple_connection_names__Different_instances()
        {
            // Arrange
            var sess1 = new SqlPhormSession(CONN_STR)
            {
                _connectionBuilder = (_, connectionName) =>
                {
                    var connMock = new Mock<IPhormDbConnection>(MockBehavior.Strict);
                    connMock.Setup(m => m.Dispose());
                    connMock.SetupProperty(m => m.DefaultSchema).Object.DefaultSchema = "dbo";
                    connMock.SetupGet(m => m.ConnectionName)
                        .Returns(connectionName);
                    return connMock.Object;
                }
            };

            // Act
            var res1 = sess1.GetConnection();

            var sess2 = (SqlPhormSession)sess1.SetConnectionName("A");
            var res2 = sess2.GetConnection();

            // Assert
            Assert.AreNotSame(res1, res2);
            Assert.IsNull(res1.ConnectionName);
            Assert.AreEqual("A", res2.ConnectionName);
        }

        [TestMethod]
        public void GetConnection__Request_closed_connection__Open_new_instance()
        {
            // Arrange
            static Mock<IPhormDbConnection> getConnMock()
            {
                var connMock = new Mock<IPhormDbConnection>(MockBehavior.Strict);
                connMock.Setup(m => m.Dispose());
                connMock.SetupProperty(m => m.DefaultSchema).Object.DefaultSchema = "dbo";
                connMock.SetupGet(m => m.State)
                    .Returns(ConnectionState.Closed);
                return connMock;
            }

            var sess = new SqlPhormSession(CONN_STR)
            {
                _connectionBuilder = (_, __) => getConnMock().Object
            };

            // Act
            var res1 = sess.GetConnection();
            var res2 = sess.GetConnection();

            // Assert
            Assert.AreNotSame(res1, res2);
        }

        [TestMethod]
        public void GetConnection__Schema_not_known__Connection_schema_used()
        {
            // Arrange
            var connMock = new Mock<IPhormDbConnection>(MockBehavior.Strict);
            connMock.Setup(m => m.Dispose());
            connMock.SetupProperty(m => m.DefaultSchema);
            connMock.SetupGet(m => m.State)
                .Returns(ConnectionState.Closed);
            connMock.Setup(m => m.Open());
            connMock.Object.DefaultSchema = string.Empty;

            var cmdMock = new Mock<IDbCommand>(MockBehavior.Strict);
            cmdMock.SetupProperty(m => m.CommandText);
            cmdMock.Setup(m => m.ExecuteScalar())
                .Returns("schema");
            cmdMock.Setup(m => m.Dispose());
            connMock.As<IDbConnection>()
                .Setup(m => m.CreateCommand())
                .Returns(cmdMock.Object);

            var sess = new SqlPhormSession(CONN_STR)
            {
                _connectionBuilder = (_, __) => connMock.Object
            };

            // Act
            var res = sess.GetConnection();

            // Assert
            Assert.AreEqual("schema", connMock.Object.DefaultSchema);
        }

        [TestMethod]
        public void GetConnection__Schema_not_known_Connection_schema_missing__Connection_UserID_used()
        {
            // Arrange
            var connMock = new Mock<IPhormDbConnection>(MockBehavior.Strict);
            connMock.Setup(m => m.Dispose());
            connMock.SetupProperty(m => m.DefaultSchema);
            connMock.SetupGet(m => m.State)
                .Returns(ConnectionState.Closed);
            connMock.Setup(m => m.Open());
            connMock.Object.DefaultSchema = string.Empty;

            var cmdMock = new Mock<IDbCommand>(MockBehavior.Strict);
            cmdMock.SetupProperty(m => m.CommandText);
            cmdMock.Setup(m => m.ExecuteScalar())
                .Returns(null); // Connection schema missing
            cmdMock.Setup(m => m.Dispose());
            connMock.As<IDbConnection>()
                .Setup(m => m.CreateCommand())
                .Returns(cmdMock.Object);

            var defaultSchema = Guid.NewGuid().ToString();

            var sess = new SqlPhormSession(CONN_STR + ";User ID=" + defaultSchema)
            {
                _connectionBuilder = (_, __) => connMock.Object
            };

            // Act
            var res = sess.GetConnection();

            // Assert
            Assert.AreEqual(defaultSchema, connMock.Object.DefaultSchema);
        }

        [TestMethod]
        public void GetConnection__Fires_Connected_event_on_new_connection()
        {
            // Arrange
            var connMock = new Mock<IPhormDbConnection>();
            connMock.Setup(m => m.Dispose());
            connMock.SetupProperty(m => m.DefaultSchema).Object.DefaultSchema = "dbo";
            connMock.SetupGet(m => m.State)
                .Returns(ConnectionState.Open);

            var sess = new SqlPhormSession(CONN_STR, null!)
            {
                _connectionBuilder = (cs, cn) => connMock.Object
            };

            int fired = 0;
            object? eventSender = null, eventArgs = null;
            sess.Connected += (s, e) =>
            {
                ++fired;
                eventSender = s;
                eventArgs = e;
            };

            // Act
            _ = sess.GetConnection();
            _ = sess.GetConnection();

            // Assert
            Assert.AreEqual(1, fired);
            Assert.AreSame(sess, eventSender);
            Assert.AreSame(connMock.Object, eventArgs);
        }

        [TestMethod]
        public void GetConnection__Connected_event_fails__Ignored()
        {
            // Arrange
            var connMock = new Mock<IPhormDbConnection>();
            connMock.Setup(m => m.Dispose());
            connMock.SetupProperty(m => m.DefaultSchema).Object.DefaultSchema = "dbo";

            var sess = new SqlPhormSession(CONN_STR, null!)
            {
                _connectionBuilder = (cs, cn) => connMock.Object
            };

            sess.Connected += (s, e) =>
            {
                throw new InvalidOperationException();
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