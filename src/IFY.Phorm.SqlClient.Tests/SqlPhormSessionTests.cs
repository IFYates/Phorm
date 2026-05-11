using IFY.Phorm.Connectivity;
using IFY.Phorm.Execution;
using IFY.Phorm.Tests;
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
        connMock.Setup(m => m.OpenAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        connMock.Setup(m => m.BeginTransactionAsync(It.IsAny<CancellationToken>()))
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
        return typeof(T).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(inst);
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
        Assert.Contains(CONN_STR, connStrUsed);
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
        var conn = sess.GetConnection(true);

        // Assert
        Assert.Contains("Application Intent=ReadOnly", connStrUsed);
    }

    [TestMethod]
    public async Task GetConnection__First_connection__Gets_new_instance()
    {
        // Arrange
        var connMock = new Mock<IAsyncDbConnection>(MockBehavior.Strict);
        connMock.Setup(m => m.Dispose());
        connMock.SetupGet(m => m.State)
            .Returns(ConnectionState.Closed);
        connMock.Setup(m => m.OpenAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var connName = Guid.NewGuid().ToString();

        var connStrUsed = string.Empty;
        var sess = new SqlPhormSession(CONN_STR, connName)
        {
            _connectionBuilder = (cs) =>
            {
                connStrUsed = cs;
                return connMock.Object;
            }
        };

        // Act
        var conn = sess.GetConnection(false);
        conn.DefaultSchema = "dbo";
        await conn.OpenAsync(default);

        // Assert
        Assert.Contains("Application Name=" + connName, connStrUsed);
        Assert.AreEqual(connName, conn.ConnectionName);
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
    public async Task GetConnection__Schema_not_known__Connection_schema_used()
    {
        // Arrange
        var connMock = new Mock<IAsyncDbConnection>(MockBehavior.Strict);
        connMock.Setup(m => m.Dispose());
        connMock.SetupGet(m => m.State)
            .Returns(ConnectionState.Closed);
        connMock.Setup(m => m.OpenAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var cmdMock = new Mock<IAsyncDbCommand>(MockBehavior.Strict);
        cmdMock.SetupProperty(m => m.CommandText);
        cmdMock.Setup(m => m.ExecuteScalarAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync("schema");
        cmdMock.Setup(m => m.Dispose());
        connMock.Setup(m => m.CreateCommand())
            .Returns(cmdMock.Object);

        var sess = new SqlPhormSession(CONN_STR)
        {
            _connectionBuilder = (_) => connMock.Object
        };

        // Act
        var conn = sess.GetConnection(false);
        await conn.OpenAsync(default);

        // Assert
        Assert.AreEqual("schema", conn.DefaultSchema);
    }

    [TestMethod]
    public async Task GetConnection__Schema_not_known_Connection_schema_missing__Connection_UserID_used()
    {
        // Arrange
        var connMock = new Mock<IAsyncDbConnection>(MockBehavior.Strict);
        connMock.Setup(m => m.Dispose());
        connMock.SetupGet(m => m.State)
            .Returns(ConnectionState.Closed);
        connMock.Setup(m => m.OpenAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var cmdMock = new Mock<IAsyncDbCommand>(MockBehavior.Strict);
        cmdMock.SetupProperty(m => m.CommandText);
        cmdMock.Setup(m => m.ExecuteScalarAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(null!); // Connection schema missing
        cmdMock.Setup(m => m.Dispose());
        connMock.Setup(m => m.CreateCommand())
            .Returns(cmdMock.Object);

        var defaultSchema = Guid.NewGuid().ToString();

        var sess = new SqlPhormSession(CONN_STR + ";User ID=" + defaultSchema)
        {
            _connectionBuilder = (_) => connMock.Object
        };

        // Act
        var conn = sess.GetConnection(false);
        await conn.OpenAsync(default);

        // Assert
        Assert.AreEqual(defaultSchema, conn.DefaultSchema);
    }

    [TestMethod]
    public async Task GetConnection__Fires_Connected_event_on_new_connection()
    {
        // Arrange
        var connMock = new Mock<IAsyncDbConnection>();
        connMock.Setup(m => m.Dispose());
        connMock.SetupGet(m => m.State).Returns(ConnectionState.Closed);
        connMock.Setup(m => m.OpenAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

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
        var conn = sess.GetConnection(false);
        conn.DefaultSchema = "dbo";
        await conn.OpenAsync(default);
        connMock.SetupGet(m => m.State).Returns(ConnectionState.Open);
        await conn.OpenAsync(default);

        // Assert
        Assert.AreEqual(1, fired);
        Assert.AreSame(sess, eventSender);
        Assert.AreSame(conn, eventArgs);
    }

    [TestMethod]
    public async Task GetConnection__Has_ContextData__Sets_on_first_open()
    {
        // Arrange
        var connMock = new Mock<IAsyncDbConnection>(MockBehavior.Strict);
        connMock.Setup(m => m.Dispose());
        connMock.SetupGet(m => m.State)
            .Returns(ConnectionState.Closed);
        connMock.Setup(m => m.OpenAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var cmdMock = new Mock<IAsyncDbCommand>(MockBehavior.Strict);
        cmdMock.SetupProperty(m => m.CommandText);
        cmdMock.Setup(m => m.ExecuteNonQueryAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        cmdMock.Setup(m => m.Dispose());
        connMock.Setup(m => m.CreateCommand())
            .Returns(cmdMock.Object);

        var paramCollection = new TestDataParameterCollection();
        cmdMock.SetupGet(m => m.Parameters).Returns(paramCollection);
        cmdMock.Setup(m => m.CreateParameter())
            .Returns(() =>
            {
                var paramMock = new Mock<IDbDataParameter>();
                paramMock.SetupAllProperties();
                return paramMock.Object;
            });

        var sess = new SqlPhormSession(CONN_STR)
        {
            _connectionBuilder = (_) => connMock.Object,
            ContextData = new Dictionary<string, object?>
            {
                { "key1", 123 },
                { "key2", "value2" }
            }
        };

        // Act
        var conn = sess.GetConnection(false);
        conn.DefaultSchema = "dbo";
        await conn.OpenAsync(default);

        // Assert
        Assert.HasCount(4, paramCollection);
        Assert.AreEqual("key1", ((IDbDataParameter)paramCollection["@keyParam0"]).Value);
        Assert.AreEqual(123, ((IDbDataParameter)paramCollection["@valueParam0"]).Value);
        Assert.AreEqual("key2", ((IDbDataParameter)paramCollection["@keyParam1"]).Value);
        Assert.AreEqual("value2", ((IDbDataParameter)paramCollection["@valueParam1"]).Value);
        Assert.Contains("EXEC sp_set_session_context @keyParam0, @valueParam0;", cmdMock.Object.CommandText);
        Assert.Contains("EXEC sp_set_session_context @keyParam1, @valueParam1;", cmdMock.Object.CommandText);
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

    [TestMethod]
    public void WithContext__Provides_new_session_with_values_set()
    {
        // Arrange
        var sess1 = new SqlPhormSession(null!);

        // Act
        var sess2 = sess1.WithContext("connName", new Dictionary<string, object?>
        {
            { "key1", 123 },
            { "key2", "value2" }
        });

        // Assert
        Assert.AreNotSame(sess1, sess2);
        Assert.IsNull(sess1.ConnectionName);
        Assert.IsEmpty(sess1.ContextData);
        Assert.AreEqual("connName", sess2.ConnectionName);
        Assert.AreEqual(123, sess2.ContextData["key1"]);
        Assert.AreEqual("value2", sess2.ContextData["key2"]);
    }

    [TestMethod]
    public async Task WithContext__Persists_session_configuration()
    {
        // Arrange
        var phorm = new SqlPhormSession(@"Server=(localdb)\ProjectModels;Database=PhormTests;MultipleActiveResultSets=True", "TestContext")
        {
            ExceptionsAsConsoleMessage = true,
            StrictResultSize = true,
            ProcedurePrefix = "TestProcPrefix_",
            TablePrefix = "TestTablePrefix_",
            ViewPrefix = "TestViewPrefix_"
        };

        // Act
        var sess = (SqlPhormSession)phorm.WithContext(null, new Dictionary<string, object?>());

        // Assert
        Assert.IsNull(sess.ConnectionName);
        Assert.AreEqual(phorm.ExceptionsAsConsoleMessage, sess.ExceptionsAsConsoleMessage);
        Assert.AreEqual(phorm.StrictResultSize, sess.StrictResultSize);
        Assert.AreEqual(phorm.ProcedurePrefix, sess.ProcedurePrefix);
        Assert.AreEqual(phorm.TablePrefix, sess.TablePrefix);
        Assert.AreEqual(phorm.ViewPrefix, sess.ViewPrefix);
    }

    [TestMethod]
    public async Task WithContext__Persists_event_handlers()
    {
        // Arrange
        var sess1 = new SqlPhormSession(null!);

        var calledConnected = false;
        sess1.Connected += (s, e) =>
        {
            calledConnected = true;
        };

        var calledExecuting = false;
        sess1.CommandExecuting += (s, e) =>
        {
            calledExecuting = true;
        };

        var calledExecuted = false;
        sess1.CommandExecuted += (s, e) =>
        {
            calledExecuted = true;
        };

        var calledUnexpectedColumn = false;
        sess1.UnexpectedRecordColumn += (s, e) =>
        {
            calledUnexpectedColumn = true;
        };

        var calledUnresolvedMember = false;
        sess1.UnresolvedContractMember += (s, e) =>
        {
            calledUnresolvedMember = true;
        };

        var calledConsoleMessage = false;
        sess1.ConsoleMessage += (s, e) =>
        {
            calledConsoleMessage = true;
        };

        // Act
        var sess2 = (SqlPhormSession)sess1.WithContext(null, new Dictionary<string, object?>());
        sess2.OnConnected(null!);
        sess2.OnCommandExecuting(null!);
        sess2.OnCommandExecuted(null!);
        sess2.OnUnexpectedRecordColumn(null!);
        sess2.OnUnresolvedContractMember(null!);
        sess2.OnConsoleMessage(null!);

        // Assert
        Assert.IsTrue(calledConnected);
        Assert.IsTrue(calledExecuting);
        Assert.IsTrue(calledExecuted);
        Assert.IsTrue(calledUnexpectedColumn);
        Assert.IsTrue(calledUnresolvedMember);
        Assert.IsTrue(calledConsoleMessage);
    }
}