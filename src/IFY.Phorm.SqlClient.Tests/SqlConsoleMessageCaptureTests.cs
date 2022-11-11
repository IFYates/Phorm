using IFY.Phorm.EventArgs;
using IFY.Phorm.Execution;
using IFY.Phorm.SqlClient.Tests.Helpers;
using Microsoft.Data.SqlClient;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace IFY.Phorm.SqlClient.Tests;

[TestClass]
public class SqlConsoleMessageCaptureTests
{
    [TestMethod]
    public void ProcessException__SqlException__Logs_error()
    {
        // Arrange
        var sessionMock = new Mock<AbstractPhormSession>(MockBehavior.Strict, null!, null!);
        var conn = new SqlConnection();

        var obj = new SqlConsoleMessageCapture(sessionMock.Object, Guid.Empty, conn);

        var errs = MicrosoftDataSqlClientHelpers.NewSqlErrorCollection();
        var err = MicrosoftDataSqlClientHelpers.NewSqlError(1, 2, 3, "err server", "err message", "err procedure", 7, 8, new Exception("inner error"));
        MicrosoftDataSqlClientHelpers.AddErrorToCollection(errs, err);

        var ex = MicrosoftDataSqlClientHelpers.NewSqlException("message", errs);

        // Act
        var res = obj.ProcessException(ex);

        // Assert
        Assert.IsTrue(res);
        Assert.IsTrue(obj.HasError);

        var msgs = obj.GetConsoleMessages();
        Assert.AreEqual(1, msgs.Length);
        Assert.AreEqual("err message", msgs[0].Message);
        Assert.AreEqual("err procedure @ 7", msgs[0].Source);
        Assert.AreEqual(3, msgs[0].Level);
        Assert.IsTrue(msgs[0].IsError);
    }

    [TestMethod]
    public void ProcessException__SqlException__Sends_event()
    {
        // Arrange
        var sessionMock = new Mock<AbstractPhormSession>(MockBehavior.Strict, null!, null!);
        var conn = new SqlConnection();

        var cmdGuid = Guid.NewGuid();

        var obj = new SqlConsoleMessageCapture(sessionMock.Object, cmdGuid, conn);

        var errs = MicrosoftDataSqlClientHelpers.NewSqlErrorCollection();
        var err = MicrosoftDataSqlClientHelpers.NewSqlError(1, 2, 3, "err server", "err message", "err procedure", 7, 8, new Exception("inner error"));
        MicrosoftDataSqlClientHelpers.AddErrorToCollection(errs, err);

        var ex = MicrosoftDataSqlClientHelpers.NewSqlException("message", errs);

        var events = new List<ConsoleMessageEventArgs>();
        sessionMock.Object.ConsoleMessage += (_, e) => events.Add(e);

        // Act
        var res = obj.ProcessException(ex);

        // Assert
        Assert.IsTrue(res);
        Assert.IsTrue(obj.HasError);

        Assert.AreEqual(1, events.Count);
        Assert.AreEqual(cmdGuid, events[0].CommandGuid);
        Assert.AreEqual("err message", events[0].ConsoleMessage.Message);
        Assert.AreEqual("err procedure @ 7", events[0].ConsoleMessage.Source);
        Assert.AreEqual(3, events[0].ConsoleMessage.Level);
        Assert.IsTrue(events[0].ConsoleMessage.IsError);
    }

    [TestMethod]
    public void ProcessException__Not_SqlException__False()
    {
        // Arrange
        var sessionMock = new Mock<AbstractPhormSession>(MockBehavior.Strict, null!, null!);
        var conn = new SqlConnection();

        var obj = new SqlConsoleMessageCapture(sessionMock.Object, Guid.Empty, conn);

        // Act
        var res = obj.ProcessException(new InvalidOperationException());

        // Assert
        Assert.IsFalse(res);

        var msgs = obj.GetConsoleMessages();
        Assert.AreEqual(0, msgs.Length);
    }

    [TestMethod]
    public void InfoMessage__Logs_event()
    {
        // Arrange
        var sessionMock = new Mock<AbstractPhormSession>(MockBehavior.Strict, null!, null!);
        var conn = new SqlConnection();

        var obj = new SqlConsoleMessageCapture(sessionMock.Object, Guid.Empty, conn);

        var errs = MicrosoftDataSqlClientHelpers.NewSqlErrorCollection();
        var err = MicrosoftDataSqlClientHelpers.NewSqlError(1, 2, 3, "err server", "err message", "err procedure", 7, 8, new Exception("inner error"));
        MicrosoftDataSqlClientHelpers.AddErrorToCollection(errs, err);

        var ex = MicrosoftDataSqlClientHelpers.NewSqlException("message", errs);

        var e = MicrosoftDataSqlClientHelpers.NewSqlInfoMessageEventArgs(ex);

        // Act
        MicrosoftDataSqlClientHelpers.FireInfoMessageEvent(conn, e);

        // Assert
        Assert.IsFalse(obj.HasError);

        var msgs = obj.GetConsoleMessages();
        Assert.AreEqual(1, msgs.Length);
        Assert.AreEqual("err message", msgs[0].Message);
        Assert.AreEqual("err procedure @ 7", msgs[0].Source);
        Assert.AreEqual(3, msgs[0].Level);
        Assert.IsFalse(msgs[0].IsError);
    }

    [TestMethod]
    public void InfoMessage__Sends_event()
    {
        // Arrange
        var sessionMock = new Mock<AbstractPhormSession>(MockBehavior.Strict, null!, null!);
        var conn = new SqlConnection();

        var cmdGuid = Guid.NewGuid();

        var obj = new SqlConsoleMessageCapture(sessionMock.Object, cmdGuid, conn);
        
        var errs = MicrosoftDataSqlClientHelpers.NewSqlErrorCollection();
        var err = MicrosoftDataSqlClientHelpers.NewSqlError(1, 2, 3, "err server", "err message", "err procedure", 7, 8, new Exception("inner error"));
        MicrosoftDataSqlClientHelpers.AddErrorToCollection(errs, err);

        var ex = MicrosoftDataSqlClientHelpers.NewSqlException("message", errs);

        var events = new List<ConsoleMessageEventArgs>();
        sessionMock.Object.ConsoleMessage += (_, e) => events.Add(e);

        var e = MicrosoftDataSqlClientHelpers.NewSqlInfoMessageEventArgs(ex);

        // Act
        MicrosoftDataSqlClientHelpers.FireInfoMessageEvent(conn, e);

        // Assert
        Assert.IsFalse(obj.HasError);

        Assert.AreEqual(1, events.Count);
        Assert.AreEqual(cmdGuid, events[0].CommandGuid);
        Assert.AreEqual("err message", events[0].ConsoleMessage.Message);
        Assert.AreEqual("err procedure @ 7", events[0].ConsoleMessage.Source);
        Assert.AreEqual(3, events[0].ConsoleMessage.Level);
        Assert.IsFalse(events[0].ConsoleMessage.IsError);
    }

    [TestMethod]
    public void Dispose__Unsubscribes_event()
    {
        // Arrange
        var sessionMock = new Mock<AbstractPhormSession>(MockBehavior.Strict, null!, null!);
        var conn = new SqlConnection();

        var obj = new SqlConsoleMessageCapture(sessionMock.Object, Guid.Empty, conn);

        var errs = MicrosoftDataSqlClientHelpers.NewSqlErrorCollection();
        var err = MicrosoftDataSqlClientHelpers.NewSqlError(1, 2, 3, "err server", "err message", "err procedure", 7, 8, new Exception("inner error"));
        MicrosoftDataSqlClientHelpers.AddErrorToCollection(errs, err);

        var ex = MicrosoftDataSqlClientHelpers.NewSqlException("message", errs);

        var events = new List<ConsoleMessageEventArgs>();
        sessionMock.Object.ConsoleMessage += (_, e) => events.Add(e);

        var e = MicrosoftDataSqlClientHelpers.NewSqlInfoMessageEventArgs(ex);

        // Act
        obj.Dispose();
        MicrosoftDataSqlClientHelpers.FireInfoMessageEvent(conn, e);

        // Assert
        Assert.AreEqual(0, events.Count);
        Assert.IsFalse(obj.HasError);
    }
}