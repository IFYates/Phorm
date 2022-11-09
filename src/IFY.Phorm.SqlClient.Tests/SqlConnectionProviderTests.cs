using IFY.Phorm.Connectivity;
using Microsoft.Data.SqlClient;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;

namespace IFY.Phorm.SqlClient.Tests
{
    // TODO: change to SqlPhormSession / AbstractPhormSession tests
    //[TestClass]
    //public class SqlConnectionProviderTests
    //{
    //    private const string DB_NAME = "PhormTests";
    //    private const string CONN_STR = @"Server=(localdb)\ProjectModels;Database=" + DB_NAME + ";";

    //    [TestInitialize]
    //    public void Init()
    //    {
    //        // Reset static state
    //        ((Dictionary<string, IPhormDbConnection>)typeof(SqlPhormSession)
    //            .GetField("_connectionPool", BindingFlags.Static | BindingFlags.NonPublic)!
    //            .GetValue(null)!).Clear();
    //    }

    //    [TestMethod]
    //    public void GetConnection__First_connection__Gets_new_instance()
    //    {
    //        // Arrange
    //        var connMock = new Mock<IPhormDbConnection>(MockBehavior.Strict);
    //        connMock.SetupGet(m => m.DefaultSchema)
    //            .Returns("dbo");

    //        var connProv = new SqlConnectionProvider(CONN_STR);

    //        string? connectionNameUsed = null;
    //        IDbConnection? connectionUsed = null;
    //        connProv._connectionBuilder = (connectionName, conn) =>
    //        {
    //            connectionNameUsed = connectionName;
    //            connectionUsed = conn;
    //            return connMock.Object;
    //        };

    //        // Act
    //        var res = connProv.GetConnection(null);

    //        // Assert
    //        connMock.VerifyAll();
    //        Assert.AreEqual(DB_NAME, connectionUsed!.Database);
    //        Assert.AreEqual(null, connectionNameUsed);
    //        Assert.AreSame(connMock.Object, res);
    //    }

    //    [TestMethod]
    //    public void GetConnection__Repeat_connection__Gets_same_open_instance()
    //    {
    //        // Arrange
    //        var mocks = new MockRepository(MockBehavior.Strict);

    //        Mock<IPhormDbConnection> getConnMock()
    //        {
    //            var connMock = mocks.Create<IPhormDbConnection>(MockBehavior.Strict);
    //            connMock.SetupGet(m => m.DefaultSchema)
    //                .Returns("dbo");
    //            connMock.SetupGet(m => m.State)
    //                .Returns(ConnectionState.Open);
    //            return connMock;
    //        }

    //        var connProv = new SqlConnectionProvider(CONN_STR)
    //        {
    //            _connectionBuilder = (_, __) => getConnMock().Object
    //        };

    //        // Act
    //        var res1 = connProv.GetConnection(null);
    //        var res2 = connProv.GetConnection(null);

    //        // Assert
    //        mocks.VerifyAll();
    //        Assert.AreSame(res1, res2);
    //    }

    //    [TestMethod]
    //    public void GetConnection__Multiple_connection_names__Different_instances()
    //    {
    //        // Arrange
    //        var connProv = new SqlConnectionProvider(CONN_STR)
    //        {
    //            _connectionBuilder = (connectionName, _) =>
    //            {
    //                var connMock = new Mock<IPhormDbConnection>(MockBehavior.Strict);
    //                connMock.SetupGet(m => m.DefaultSchema)
    //                    .Returns("dbo");
    //                connMock.SetupGet(m => m.ConnectionName)
    //                    .Returns(connectionName);
    //                return connMock.Object;
    //            }
    //        };

    //        // Act
    //        var res1 = connProv.GetConnection(null);
    //        var res2 = connProv.GetConnection("A");

    //        // Assert
    //        Assert.AreNotSame(res1, res2);
    //        Assert.IsNull(res1.ConnectionName);
    //        Assert.AreEqual("A", res2.ConnectionName);
    //    }

    //    [TestMethod]
    //    public void GetConnection__Repeat_closed_connection__Open_new_instance()
    //    {
    //        // Arrange
    //        var mocks = new MockRepository(MockBehavior.Strict);

    //        Mock<IPhormDbConnection> getConnMock()
    //        {
    //            var connMock = mocks.Create<IPhormDbConnection>(MockBehavior.Strict);
    //            connMock.SetupGet(m => m.DefaultSchema)
    //                .Returns("dbo");
    //            connMock.SetupGet(m => m.State)
    //                .Returns(ConnectionState.Closed);
    //            connMock.Setup(m => m.Dispose());
    //            return connMock;
    //        }

    //        var connProv = new SqlConnectionProvider(CONN_STR)
    //        {
    //            _connectionBuilder = (_, __) => getConnMock().Object
    //        };

    //        // Act
    //        var res1 = connProv.GetConnection(null);
    //        var res2 = connProv.GetConnection(null);

    //        // Assert
    //        Assert.AreNotSame(res1, res2);
    //    }

    //    [TestMethod]
    //    public void GetConnection__Schema_not_known__Connection_schema_used()
    //    {
    //        // Arrange
    //        var connMock = new Mock<IPhormDbConnection>(MockBehavior.Strict);
    //        connMock.SetupProperty(m => m.DefaultSchema);
    //        connMock.SetupGet(m => m.State)
    //            .Returns(ConnectionState.Closed);
    //        connMock.Setup(m => m.Open());
    //        connMock.Setup(m => m.Dispose());
    //        connMock.Object.DefaultSchema = string.Empty;

    //        var cmdMock = new Mock<IDbCommand>(MockBehavior.Strict);
    //        cmdMock.SetupProperty(m => m.CommandText);
    //        cmdMock.Setup(m => m.ExecuteScalar())
    //            .Returns("schema");
    //        cmdMock.Setup(m => m.Dispose());
    //        connMock.As<IDbConnection>()
    //            .Setup(m => m.CreateCommand())
    //            .Returns(cmdMock.Object);

    //        var connProv = new SqlConnectionProvider(CONN_STR)
    //        {
    //            _connectionBuilder = (_, __) => connMock.Object
    //        };

    //        // Act
    //        var res = connProv.GetConnection(null);

    //        // Assert
    //        Assert.AreEqual("schema", connMock.Object.DefaultSchema);
    //    }

    //    [TestMethod]
    //    public void GetConnection__Schema_not_known_Connection_schema_missing__Connection_UserID_used()
    //    {
    //        // Arrange
    //        var connMock = new Mock<IPhormDbConnection>(MockBehavior.Strict);
    //        connMock.SetupProperty(m => m.DefaultSchema);
    //        connMock.SetupGet(m => m.State)
    //            .Returns(ConnectionState.Closed);
    //        connMock.Setup(m => m.Open());
    //        connMock.Setup(m => m.Dispose());
    //        connMock.Object.DefaultSchema = string.Empty;

    //        var cmdMock = new Mock<IDbCommand>(MockBehavior.Strict);
    //        cmdMock.SetupProperty(m => m.CommandText);
    //        cmdMock.Setup(m => m.ExecuteScalar())
    //            .Returns(null); // Connection schema missing
    //        cmdMock.Setup(m => m.Dispose());
    //        connMock.As<IDbConnection>()
    //            .Setup(m => m.CreateCommand())
    //            .Returns(cmdMock.Object);

    //        string? connectionString = null;
    //        var connProv = new SqlConnectionProvider(CONN_STR)
    //        {
    //            _connectionBuilder = (_, conn) =>
    //            {
    //                connectionString = conn.ConnectionString;
    //                return connMock.Object;
    //            }
    //        };

    //        // Act
    //        var res = connProv.GetConnection(null);

    //        // Assert
    //        Assert.AreEqual(new SqlConnectionStringBuilder(connectionString).UserID, connMock.Object.DefaultSchema);
    //    }

    //    [TestMethod]
    //    public void GetConnection__Invokes_Connected_event()
    //    {
    //        // Arrange
    //        var mocks = new MockRepository(MockBehavior.Strict);

    //        Mock<IPhormDbConnection> getConnMock()
    //        {
    //            var connMock = mocks.Create<IPhormDbConnection>(MockBehavior.Strict);
    //            connMock.SetupGet(m => m.DefaultSchema)
    //                .Returns("dbo");
    //            connMock.SetupGet(m => m.State)
    //                .Returns(ConnectionState.Closed);
    //            connMock.Setup(m => m.Dispose());
    //            return connMock;
    //        }

    //        var connProv = new SqlConnectionProvider(CONN_STR)
    //        {
    //            _connectionBuilder = (_, __) => getConnMock().Object
    //        };

    //        // Act
    //        var fired = 0;
    //        connProv.Connected += (sender, args) =>
    //        {
    //            ++fired;
    //        };
    //        _ = connProv.GetConnection(null);
    //        _ = connProv.GetConnection(null);

    //        // Assert
    //        Assert.AreEqual(2, fired);
    //    }

    //    [TestMethod]
    //    public void GetConnection__Connected_event_fails__Consumed()
    //    {
    //        // Arrange
    //        var mocks = new MockRepository(MockBehavior.Strict);

    //        Mock<IPhormDbConnection> getConnMock()
    //        {
    //            var connMock = mocks.Create<IPhormDbConnection>(MockBehavior.Strict);
    //            connMock.SetupGet(m => m.DefaultSchema)
    //                .Returns("dbo");
    //            connMock.SetupGet(m => m.State)
    //                .Returns(ConnectionState.Closed);
    //            connMock.Setup(m => m.Dispose());
    //            return connMock;
    //        }

    //        var connProv = new SqlConnectionProvider(CONN_STR)
    //        {
    //            _connectionBuilder = (_, __) => getConnMock().Object
    //        };

    //        // Act
    //        connProv.Connected += (sender, args) =>
    //        {
    //            throw new InvalidOperationException();
    //        };
    //        var res = connProv.GetConnection(null);

    //        // Assert
    //        Assert.IsNotNull(res);
    //    }

    //    [TestMethod]
    //    [DataRow("NULL")]
    //    [DataRow("")]
    //    [DataRow("test")]
    //    public void GetSession(string? connectionName)
    //    {
    //        // Arrange
    //        if (connectionName == "NULL")
    //        {
    //            connectionName = null; // Fix for DataRow(null) not executing test
    //        }
    //        var connProv = new SqlConnectionProvider(string.Empty);

    //        // Act
    //        var res = connProv.GetSession(connectionName);

    //        // Assert
    //        Assert.IsInstanceOfType(res, typeof(SqlPhormSession));
    //        var sessionConnectionName = (string?)typeof(SqlPhormSession).GetMethod("GetConnectionName", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.Invoke(res, null);
    //        Assert.AreEqual(connectionName, sessionConnectionName);
    //    }
    //}
}