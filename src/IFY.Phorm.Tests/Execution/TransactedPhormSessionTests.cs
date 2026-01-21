using IFY.Phorm.Data;
using IFY.Phorm.Tests;
using Moq;
using System.Data;

namespace IFY.Phorm.Execution.Tests;

[TestClass]
public class TransactedPhormSessionTests
{
    public TestContext TestContext { get; set; }

    private readonly MockRepository _mocks = new(MockBehavior.Strict);

    [TestMethod]
    public void IsInTransaction__True()
    {
        // Arrange
        var sessMock = _mocks.Create<AbstractPhormSession>(null!, null!);

        var sess = new TransactedPhormSession(sessMock.Object, null!);

        // Assert
        _mocks.Verify();
        Assert.IsTrue(sess.IsInTransaction);
    }

    [TestMethod]
    public void Commit()
    {
        // Arrange
        var transMock = _mocks.Create<IDbTransaction>();
        transMock.Setup(m => m.Commit()).Verifiable();

        var sess = new TransactedPhormSession(null!, transMock.Object);

        // Act
        sess.Commit();

        // Assert
        _mocks.Verify();
        Assert.IsTrue(sess.IsInTransaction);
    }

    [TestMethod]
    public void Rollback()
    {
        // Arrange
        var transMock = _mocks.Create<IDbTransaction>();
        transMock.Setup(m => m.Rollback()).Verifiable();

        var sess = new TransactedPhormSession(null!, transMock.Object);

        // Act
        sess.Rollback();

        // Assert
        _mocks.Verify();
    }

    [TestMethod]
    public void Dispose()
    {
        // Arrange
        var transMock = _mocks.Create<IDbTransaction>();
        transMock.Setup(m => m.Dispose()).Verifiable();

        var sess = new TransactedPhormSession(null!, transMock.Object);

        // Act
        sess.Dispose();

        // Assert
        _mocks.Verify();
    }

    interface ITestAction : IPhormContract
    {
    }

    [TestMethod]
    public async Task CallAsync__Has_transaction_set()
    {
        // Arrange
        var baseSession = new TestPhormSession();

        var dbtranMock = _mocks.Create<IDbTransaction>();

        var sess = new TransactedPhormSession(baseSession, dbtranMock.Object);

        // Act
        await sess.CallAsync("contract", null, TestContext.CancellationToken);

        // Assert
        var cmd = baseSession.Commands.Single();
        Assert.AreSame(dbtranMock.Object, cmd.Transaction);
    }

    [TestMethod]
    public async Task CallAsync_T__Has_transaction_set()
    {
        // Arrange
        var baseSession = new TestPhormSession();

        var dbtranMock = _mocks.Create<IDbTransaction>();

        var sess = new TransactedPhormSession(baseSession, dbtranMock.Object);

        // Act
        await sess.CallAsync<ITestAction>(null, TestContext.CancellationToken);

        // Assert
        var cmd = baseSession.Commands.Single();
        Assert.AreSame(dbtranMock.Object, cmd.Transaction);
    }

    [TestMethod]
    public async Task From__Has_transaction_set()
    {
        // Arrange
        var baseSession = new TestPhormSession();

        var dbtranMock = _mocks.Create<IDbTransaction>();

        var sess = new TransactedPhormSession(baseSession, dbtranMock.Object);

        // Act
        var runner = sess.From("contract", null);
        await runner.GetAsync<object>(TestContext.CancellationToken);

        // Assert
        var cmd = baseSession.Commands.Single();
        Assert.AreSame(dbtranMock.Object, cmd.Transaction);
    }

    [TestMethod]
    public async Task From_T__Has_transaction_set()
    {
        // Arrange
        var baseSession = new TestPhormSession();

        var dbtranMock = _mocks.Create<IDbTransaction>();

        var sess = new TransactedPhormSession(baseSession, dbtranMock.Object);

        // Act
        var runner = sess.From<ITestAction>(null);
        await runner.GetAsync<object>(TestContext.CancellationToken);

        // Assert
        var cmd = baseSession.Commands.Single();
        Assert.AreSame(dbtranMock.Object, cmd.Transaction);
    }

    [TestMethod]
    public async Task GetAsync_T__Has_transaction_set()
    {
        // Arrange
        var baseSession = new TestPhormSession();

        var dbtranMock = _mocks.Create<IDbTransaction>();

        var sess = new TransactedPhormSession(baseSession, dbtranMock.Object);

        // Act
        await sess.GetAsync<object>(null, TestContext.CancellationToken);

        // Assert
        var cmd = baseSession.Commands.Single();
        Assert.AreSame(dbtranMock.Object, cmd.Transaction);
    }
}