using IFY.Phorm.Data;
using IFY.Phorm.Tests;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Data;

namespace IFY.Phorm.Execution.Tests;

[TestClass]
public class TransactedPhormSessionTests
{
    private readonly MockRepository _mocks = new(MockBehavior.Strict);

    [TestInitialize]
    public void Init()
    {
        AbstractPhormSession.ResetConnectionPool();
    }

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
    public void CallAsync__Has_transaction_set()
    {
        // Arrange
        var baseSession = new TestPhormSession();

        var dbtranMock = _mocks.Create<IDbTransaction>();

        var sess = new TransactedPhormSession(baseSession, dbtranMock.Object);

        // Act
        _ = sess.CallAsync("contract", null, CancellationToken.None).Result;

        // Assert
        var cmd = baseSession.Commands.Single();
        Assert.AreSame(dbtranMock.Object, cmd.Transaction);
    }

    [TestMethod]
    public void CallAsync_T__Has_transaction_set()
    {
        // Arrange
        var baseSession = new TestPhormSession();

        var dbtranMock = _mocks.Create<IDbTransaction>();

        var sess = new TransactedPhormSession(baseSession, dbtranMock.Object);

        // Act
        _ = sess.CallAsync<ITestAction>(null, CancellationToken.None).Result;

        // Assert
        var cmd = baseSession.Commands.Single();
        Assert.AreSame(dbtranMock.Object, cmd.Transaction);
    }

    [TestMethod]
    public void From__Has_transaction_set()
    {
        // Arrange
        var baseSession = new TestPhormSession();

        var dbtranMock = _mocks.Create<IDbTransaction>();

        var sess = new TransactedPhormSession(baseSession, dbtranMock.Object);

        // Act
        var runner = sess.From("contract", null);
        _ = runner.Get<object>();

        // Assert
        var cmd = baseSession.Commands.Single();
        Assert.AreSame(dbtranMock.Object, cmd.Transaction);
    }

    [TestMethod]
    public void From_T__Has_transaction_set()
    {
        // Arrange
        var baseSession = new TestPhormSession();

        var dbtranMock = _mocks.Create<IDbTransaction>();

        var sess = new TransactedPhormSession(baseSession, dbtranMock.Object);

        // Act
        var runner = sess.From<ITestAction>(null);
        _ = runner.Get<object>();

        // Assert
        var cmd = baseSession.Commands.Single();
        Assert.AreSame(dbtranMock.Object, cmd.Transaction);
    }

    [TestMethod]
    public void Get_T__Has_transaction_set()
    {
        // Arrange
        var baseSession = new TestPhormSession();

        var dbtranMock = _mocks.Create<IDbTransaction>();

        var sess = new TransactedPhormSession(baseSession, dbtranMock.Object);

        // Act
        _ = sess.Get<object>(null);

        // Assert
        var cmd = baseSession.Commands.Single();
        Assert.AreSame(dbtranMock.Object, cmd.Transaction);
    }

    [TestMethod]
    public void GetAsync_T__Has_transaction_set()
    {
        // Arrange
        var baseSession = new TestPhormSession();

        var dbtranMock = _mocks.Create<IDbTransaction>();

        var sess = new TransactedPhormSession(baseSession, dbtranMock.Object);

        // Act
        _ = sess.GetAsync<object>(null, CancellationToken.None).Result;

        // Assert
        var cmd = baseSession.Commands.Single();
        Assert.AreSame(dbtranMock.Object, cmd.Transaction);
    }
}