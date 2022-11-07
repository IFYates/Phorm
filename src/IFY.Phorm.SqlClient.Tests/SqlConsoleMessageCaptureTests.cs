using IFY.Phorm.EventArgs;
using Microsoft.Data.SqlClient;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace IFY.Phorm.SqlClient.Tests
{
    [TestClass]
    public class SqlConsoleMessageCaptureTests
    {
        #region Microsoft.Data.SqlClient helpers
        private static SqlException newSqlException(string message, SqlErrorCollection errs)
        {
            return (SqlException)typeof(SqlException).GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic)
                .First().Invoke(new object[] { message, errs, null!, Guid.Empty });
        }
        private static SqlError newSqlError(int number, byte state, byte errorClass, string server, string message, string procedure, int lineNumber, uint win32ErrCode, Exception innerException)
        {
            return (SqlError)typeof(SqlError).GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic)
                .First().Invoke(new object[] { number, state, errorClass, server, message, procedure, lineNumber, win32ErrCode, innerException });
        }
        private static SqlErrorCollection newSqlErrorCollection()
        {
            return (SqlErrorCollection)Activator.CreateInstance(typeof(SqlErrorCollection), true)!;
        }
        private static void addErrorToCollection(SqlErrorCollection coll, SqlError err)
        {
            var addErr = coll.GetType().GetMethod("Add", BindingFlags.Instance | BindingFlags.NonPublic);
            addErr!.Invoke(coll, new object[] { err });
        }
        private static SqlInfoMessageEventArgs newSqlInfoMessageEventArgs(SqlException ex)
        {
            return (SqlInfoMessageEventArgs)typeof(SqlInfoMessageEventArgs).GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic)
                .First().Invoke(new object[] { ex });
        }
        private static void fireInfoMessageEvent(SqlConnection conn, SqlInfoMessageEventArgs e)
        {
            var ev = (MulticastDelegate)typeof(SqlConnection).GetField("InfoMessage", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(conn)!;
            foreach (var handler in ev?.GetInvocationList() ?? Array.Empty<Delegate>())
            {
                handler.Method.Invoke(handler.Target, new object[] { conn, e });
            }
        }
        #endregion Microsoft.Data.SqlClient helpers

        [TestMethod]
        public void ProcessException__SqlException__Logs_error()
        {
            // Arrange
            var sessionMock = new Mock<AbstractPhormSession>(MockBehavior.Strict, null!, null!);
            var conn = new SqlConnection();

            var obj = new SqlConsoleMessageCapture(sessionMock.Object, Guid.Empty, conn);

            var errs = newSqlErrorCollection();
            var err = newSqlError(1, 2, 3, "err server", "err message", "err procedure", 7, 8, new Exception("inner error"));
            addErrorToCollection(errs, err);

            var ex = newSqlException("message", errs);

            // Act
            var res = obj.ProcessException(ex);

            // Assert
            Assert.IsTrue(res);

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

            var errs = newSqlErrorCollection();
            var err = newSqlError(1, 2, 3, "err server", "err message", "err procedure", 7, 8, new Exception("inner error"));
            addErrorToCollection(errs, err);

            var ex = newSqlException("message", errs);

            var events = new List<ConsoleMessageEventArgs>();
            sessionMock.Object.ConsoleMessage += (_, e) => events.Add(e);

            // Act
            var res = obj.ProcessException(ex);

            // Assert
            Assert.IsTrue(res);
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

            var errs = newSqlErrorCollection();
            var err = newSqlError(1, 2, 3, "err server", "err message", "err procedure", 7, 8, new Exception("inner error"));
            addErrorToCollection(errs, err);

            var ex = newSqlException("message", errs);

            var e = newSqlInfoMessageEventArgs(ex);

            // Act
            fireInfoMessageEvent(conn, e);

            // Assert
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
            
            var errs = newSqlErrorCollection();
            var err = newSqlError(1, 2, 3, "err server", "err message", "err procedure", 7, 8, new Exception("inner error"));
            addErrorToCollection(errs, err);

            var ex = newSqlException("message", errs);

            var events = new List<ConsoleMessageEventArgs>();
            sessionMock.Object.ConsoleMessage += (_, e) => events.Add(e);

            var e = newSqlInfoMessageEventArgs(ex);

            // Act
            fireInfoMessageEvent(conn, e);

            // Assert
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

            var errs = newSqlErrorCollection();
            var err = newSqlError(1, 2, 3, "err server", "err message", "err procedure", 7, 8, new Exception("inner error"));
            addErrorToCollection(errs, err);

            var ex = newSqlException("message", errs);

            var events = new List<ConsoleMessageEventArgs>();
            sessionMock.Object.ConsoleMessage += (_, e) => events.Add(e);

            var e = newSqlInfoMessageEventArgs(ex);

            // Act
            obj.Dispose();
            fireInfoMessageEvent(conn, e);

            // Assert
            Assert.AreEqual(0, events.Count);
        }
    }
}