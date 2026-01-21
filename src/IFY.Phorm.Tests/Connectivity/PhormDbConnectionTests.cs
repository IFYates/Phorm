using IFY.Phorm.Tests;
using Moq;
using System.Data;

namespace IFY.Phorm.Connectivity.Tests;

[TestClass]
public class PhormDbConnectionTests
{
    [TestMethod]
    public void Properties()
    {
        // Arrange
        var dbMock = new Mock<IAsyncDbConnection>(MockBehavior.Strict);
        dbMock.SetupProperty(m => m.ConnectionString);
        dbMock.SetupGet(m => m.ConnectionTimeout).Returns(1234);
        dbMock.SetupGet(m => m.Database).Returns("databaseName");
        dbMock.SetupGet(m => m.State).Returns(ConnectionState.Connecting);

        var sess = new TestPhormSession("contextName");
        var db = new PhormDbConnection(sess, dbMock.Object)
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
    public async Task BeginTransaction()
    {
        // Arrange
        var dbMock = new Mock<IAsyncDbConnection>(MockBehavior.Strict);
        dbMock.Setup(m => m.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => null!).Verifiable();

        var db = new PhormDbConnection(new TestPhormSession(), dbMock.Object);

        // Act
        await db.BeginTransactionAsync(default);

        // Assert
        dbMock.Verify();
    }

    [TestMethod]
    public async Task BeginTransaction__With_IsolationLevel()
    {
        // Arrange
        var dbMock = new Mock<IAsyncDbConnection>(MockBehavior.Strict);
        dbMock.Setup(m => m.BeginTransactionAsync(IsolationLevel.Chaos, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => null!).Verifiable();

        var db = new PhormDbConnection(new TestPhormSession(), dbMock.Object);

        // Act
        await db.BeginTransactionAsync(IsolationLevel.Chaos, default);

        // Assert
        dbMock.Verify();
    }

    [TestMethod]
    public async Task ChangeDatabase()
    {
        // Arrange
        var dbMock = new Mock<IAsyncDbConnection>(MockBehavior.Strict);
        dbMock.Setup(m => m.ChangeDatabaseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Verifiable();

        var db = new PhormDbConnection(new TestPhormSession(), dbMock.Object);

        // Act
        await db.ChangeDatabaseAsync(string.Empty, default);

        // Assert
        dbMock.Verify();
    }

    [TestMethod]
    public async Task Open__Is_open__Noop()
    {
        // Arrange
        var dbMock = new Mock<IAsyncDbConnection>(MockBehavior.Strict);
        dbMock.SetupGet(m => m.State)
            .Returns(ConnectionState.Open).Verifiable();

        var db = new PhormDbConnection(new TestPhormSession(), dbMock.Object);

        // Act
        await db.OpenAsync(default);

        // Assert
        dbMock.Verify();
    }

    [TestMethod]
    public async Task Open__Not_open__Open()
    {
        // Arrange
        var dbMock = new Mock<IAsyncDbConnection>(MockBehavior.Strict);
        dbMock.SetupGet(m => m.State)
            .Returns(ConnectionState.Closed).Verifiable();
        dbMock.Setup(m => m.OpenAsync(It.IsAny<CancellationToken>()))
            .Verifiable();

        var db = new PhormDbConnection(new TestPhormSession(), dbMock.Object);

        // Act
        await db.OpenAsync(default);

        // Assert
        dbMock.Verify();
    }

    [TestMethod]
    public void Close__Is_closed__Noop()
    {
        // Arrange
        var dbMock = new Mock<IAsyncDbConnection>(MockBehavior.Strict);
        dbMock.SetupGet(m => m.State)
            .Returns(ConnectionState.Closed).Verifiable();

        var db = new PhormDbConnection(new TestPhormSession(), dbMock.Object);

        // Act
        db.Close();

        // Assert
        dbMock.Verify();
    }

    [TestMethod]
    public void Close__Not_closed__Close()
    {
        // Arrange
        var dbMock = new Mock<IAsyncDbConnection>(MockBehavior.Strict);
        dbMock.SetupGet(m => m.State)
            .Returns(ConnectionState.Open).Verifiable();
        dbMock.Setup(m => m.Close())
            .Verifiable();

        var db = new PhormDbConnection(new TestPhormSession(), dbMock.Object);

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

        var dbMock = new Mock<IAsyncDbConnection>(MockBehavior.Strict);
        dbMock.SetupGet(m => m.State)
            .Returns(ConnectionState.Open);
        dbMock.Setup(m => m.CreateCommand())
            .Returns(cmd).Verifiable();

        var db = new PhormDbConnection(new TestPhormSession(), dbMock.Object);

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

        var dbMock = new Mock<IAsyncDbConnection>(MockBehavior.Strict);
        dbMock.SetupGet(m => m.State)
            .Returns(ConnectionState.Closed);
        dbMock.Setup(m => m.OpenAsync(It.IsAny<CancellationToken>()))
            .Verifiable();
        dbMock.Setup(m => m.CreateCommand())
            .Returns(cmd).Verifiable();

        var db = new PhormDbConnection(new TestPhormSession(), dbMock.Object);

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

        var dbMock = new Mock<IAsyncDbConnection>(MockBehavior.Strict);
        dbMock.SetupGet(m => m.State)
            .Returns(ConnectionState.Open);
        dbMock.Setup(m => m.CreateCommand())
            .Returns(cmdMock.Object).Verifiable();

        var db = new PhormDbConnection(new TestPhormSession(), dbMock.Object);

        // Act
        var res = ((IAsyncDbConnection)db).CreateCommand();

        // Assert
        Assert.AreSame(cmdMock.Object, res);
        dbMock.Verify();
    }

    [TestMethod]
    public void CreateCommand__IDbConnection__Connection_closed__Opens_connection_and_wraps_command_as_IAsyncDbCommand()
    {
        // Arrange
        var cmdMock = new Mock<IDbCommand>(MockBehavior.Strict);

        var dbMock = new Mock<IAsyncDbConnection>(MockBehavior.Strict);
        dbMock.SetupGet(m => m.State)
            .Returns(ConnectionState.Closed);
        dbMock.Setup(m => m.OpenAsync(It.IsAny<CancellationToken>()))
            .Verifiable();
        dbMock.Setup(m => m.CreateCommand())
            .Returns(cmdMock.Object).Verifiable();

        var db = new PhormDbConnection(new TestPhormSession(), dbMock.Object);

        // Act
        var res = ((IAsyncDbConnection)db).CreateCommand();

        // Assert
        Assert.AreSame(cmdMock.Object, res);
        dbMock.Verify();
    }

    [TestMethod]
    public void Dispose()
    {
        // Arrange
        var dbMock = new Mock<IAsyncDbConnection>(MockBehavior.Strict);
        dbMock.Setup(m => m.Dispose())
            .Verifiable();

        var db = new PhormDbConnection(new TestPhormSession(), dbMock.Object);

        // Act
        db.Dispose();

        // Assert
        dbMock.Verify();
    }
}