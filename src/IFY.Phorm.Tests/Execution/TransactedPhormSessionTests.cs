using IFY.Phorm.Connectivity;
using IFY.Phorm.EventArgs;
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
    public void CreateCommand__Sets_transaction()
    {
        // Arrange
        var cmdMock = _mocks.Create<IAsyncDbCommand>();
        cmdMock.SetupAllProperties();

        var connMock = _mocks.Create<IPhormDbConnection>();

        var sessMock = _mocks.Create<AbstractPhormSession>(null!, null!);
        sessMock.Setup(m => m.CreateCommand(connMock.Object, "schema", "objectName", DbObjectType.Table))
            .Returns(cmdMock.Object).Verifiable();

        var transMock = _mocks.Create<IDbTransaction>();

        var sess = new TransactedPhormSession(sessMock.Object, transMock.Object);

        // Act
        var res = sess.CreateCommand(connMock.Object, "schema", "objectName", DbObjectType.Table);

        // Assert
        _mocks.Verify();
        Assert.AreSame(transMock.Object, res.Transaction);
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
}