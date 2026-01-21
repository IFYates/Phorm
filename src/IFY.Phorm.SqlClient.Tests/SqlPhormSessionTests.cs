using IFY.Phorm.Connectivity;
using IFY.Phorm.Execution;
using Microsoft.Data.SqlClient;
using Moq;
using System.Data;
using System.Reflection;

namespace IFY.Phorm.SqlClient.Tests;

[TestClass]
public class SqlPhormSessionTests
{
    private const string CONN_STR = "Data Source=local";

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
    public async Task BeginTransaction()
    {
        // Arrange
        var mocks = new MockRepository(MockBehavior.Strict);

        var tranMock = mocks.Create<IDbTransaction>();

        var connMock = mocks.Create<IAsyncDbConnection>();
        connMock.Setup(m => m.Dispose());
        connMock.SetupGet(m => m.State)
            .Returns(ConnectionState.Open);
        connMock.Setup(m => m.OpenAsync(default));
        connMock.Setup(m => m.BeginTransactionAsync(default))
            .ReturnsAsync(tranMock.Object).Verifiable();

        var connName = Guid.NewGuid().ToString();

        var sess = new SqlPhormSession(CONN_STR, connName)
        {
            _connectionBuilder = (cs) =>
            {
                connMock.SetupGet(m => m.ConnectionString).Returns(cs);
                return connMock.Object;
            }
        };

        // Act
        var res = (TransactedPhormSession)await sess.BeginTransactionAsync(default);

        // Assert
        mocks.Verify();
        Assert.AreSame(tranMock.Object, getField(res, "_transaction"));
        Assert.AreEqual(connName, sess.ConnectionName);
        Assert.AreEqual(connName, res.ConnectionName);
    }

    static object? getField<T>(T inst, string fieldName)
    {
        return typeof(T).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(inst);
    }

    [TestMethod]
    public void GetConnection()
    {
        // Arrange
        var connMock = new Mock<IAsyncDbConnection>();
        connMock.Setup(m => m.Dispose());

        var connStrUsed = string.Empty;
        var sess = new SqlPhormSession(CONN_STR)
        {
            _connectionBuilder = (cs) =>
            {
                connStrUsed = cs;
                return connMock.Object;
            }
        };

        // Act
        var res = sess.GetConnection(false);

        // Assert
        Assert.AreSame(connMock.Object, res);
        Assert.DoesNotContain("Application Intent=ReadOnly", connStrUsed);
    }

    [TestMethod]
    public void GetConnection__Can_add_ReadOnly_intent()
    {
        // Arrange
        var connMock = new Mock<IAsyncDbConnection>();
        connMock.Setup(m => m.Dispose());

        var connStrUsed = string.Empty;
        var sess = new SqlPhormSession(CONN_STR)
        {
            _connectionBuilder = (cs) =>
            {
                connStrUsed = cs;
                return connMock.Object;
            }
        };

        // Act
        var res = sess.GetConnection(true);

        // Assert
        Assert.AreSame(connMock.Object, res);
        Assert.Contains("Application Intent=ReadOnly", connStrUsed);
    }

    [TestMethod]
    public void GetConnection__First_connection__Gets_new_instance()
    {
        // Arrange
        var connMock = new Mock<IAsyncDbConnection>(MockBehavior.Strict);
        connMock.Setup(m => m.Dispose());

        var connName = Guid.NewGuid().ToString();

        string? connectionStringUsed = null;
        var sess = new SqlPhormSession(CONN_STR, connName)
        {
            _connectionBuilder = (connectionString) =>
            {
                connectionStringUsed = connectionString;
                return connMock.Object;
            }
        };

        // Act
        var res = sess.GetConnection(false);

        // Assert
        Assert.AreEqual(CONN_STR + ";Application Name=" + connName, connectionStringUsed);
        Assert.AreEqual(connName, res.ConnectionName);
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
            _connectionBuilder = (_) => getConnMock().Object
        };

        // Act
        var res1 = sess.GetConnection(false);
        var res2 = sess.GetConnection(false);

        // Assert
        Assert.AreSame(res1, res2);
    }

    [TestMethod]
    public void GetConnection__Multiple_connection_names__Different_instances()
    {
        // Arrange
        var sess1 = new SqlPhormSession(CONN_STR)
        {
            _connectionBuilder = (cs) =>
            {
                var connMock = new Mock<IAsyncDbConnection>(MockBehavior.Strict);
                connMock.Setup(m => m.Dispose());
                return connMock.Object;
            }
        };

        // Act
        var res1 = sess1.GetConnection(false);

        var sess2 = (SqlPhormSession)sess1.WithContext("A", new Dictionary<string, object?>());
        var res2 = sess2.GetConnection(false);

        // Assert
        Assert.AreNotSame(res1, res2);
        Assert.IsNull(res1.ConnectionName);
        Assert.AreEqual("A", res2.ConnectionName);
    }

    [TestMethod]
    public void GetConnection__Request_closed_connection__Open_new_instance()
    {
        // Arrange
        var sess = new SqlPhormSession(CONN_STR)
        {
            _connectionBuilder = (_) =>
            {
                var connMock = new Mock<IAsyncDbConnection>(MockBehavior.Strict);
                connMock.Setup(m => m.Dispose());
                connMock.SetupGet(m => m.State)
                    .Returns(ConnectionState.Closed);
                return connMock.Object;
            }
        };

        // Act
        var res1 = sess.GetConnection(false);
        var res2 = sess.GetConnection(false);

        // Assert
        Assert.AreNotSame(res1, res2);
    }

    [TestMethod]
    public void GetConnection__Schema_not_known__Connection_schema_used()
    {
        // Arrange
        var connMock = new Mock<IAsyncDbConnection>(MockBehavior.Strict);
        connMock.Setup(m => m.Dispose());
        connMock.SetupGet(m => m.State)
            .Returns(ConnectionState.Closed);
        connMock.Setup(m => m.OpenAsync(default));

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
            _connectionBuilder = (_) => connMock.Object
        };

        // Act
        var res = sess.GetConnection(false);

        // Assert
        Assert.AreEqual("schema", res.DefaultSchema);
    }

    [TestMethod]
    public void GetConnection__Schema_not_known_Connection_schema_missing__Connection_UserID_used()
    {
        // Arrange
        var connMock = new Mock<IAsyncDbConnection>(MockBehavior.Strict);
        connMock.Setup(m => m.Dispose());
        connMock.SetupGet(m => m.State)
            .Returns(ConnectionState.Closed);
        connMock.Setup(m => m.OpenAsync(default));

        var cmdMock = new Mock<IDbCommand>(MockBehavior.Strict);
        cmdMock.SetupProperty(m => m.CommandText);
        cmdMock.Setup(m => m.ExecuteScalar())
            .Returns(null!); // Connection schema missing
        cmdMock.Setup(m => m.Dispose());
        connMock.As<IDbConnection>()
            .Setup(m => m.CreateCommand())
            .Returns(cmdMock.Object);

        var defaultSchema = Guid.NewGuid().ToString();

        var sess = new SqlPhormSession(CONN_STR + ";User ID=" + defaultSchema)
        {
            _connectionBuilder = (_) => connMock.Object
        };

        // Act
        var res = sess.GetConnection(false);

        // Assert
        Assert.AreEqual(defaultSchema, res.DefaultSchema);
    }

    [TestMethod]
    public void GetConnection__Fires_Connected_event_on_new_connection()
    {
        // Arrange
        var connMock = new Mock<IAsyncDbConnection>();
        connMock.Setup(m => m.Dispose());
        connMock.SetupGet(m => m.State)
            .Returns(ConnectionState.Open);

        var sess = new SqlPhormSession(CONN_STR)
        {
            _connectionBuilder = (_) => connMock.Object
        };

        int fired = 0;
        object? eventSender = null, eventArgs = null;
        sess.Connected += (s, e) =>
        {
            ++fired;
            eventSender = s;
            eventArgs = e.Connection;
        };

        // Act
        _ = sess.GetConnection(false);
        _ = sess.GetConnection(false);

        // Assert
        Assert.AreEqual(1, fired);
        Assert.AreSame(sess, eventSender);
        Assert.AreSame(connMock.Object, eventArgs);
    }

    [TestMethod]
    public void SetConnectionName__Returns_new_session_with_updated_connection_name()
    {
        // Arrange
        var sess1 = new SqlPhormSession(null!, "name1");

        // Act
        var sess2 = (SqlPhormSession)sess1.WithContext("name2", new Dictionary<string, object?>());

        // Assert
        Assert.AreNotSame(sess2, sess1);
        Assert.AreEqual("name1", sess1.ConnectionName);
        Assert.AreEqual("name2", sess2.ConnectionName);
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