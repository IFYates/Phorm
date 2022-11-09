using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace IFY.Phorm.Tests
{
    [TestClass]
    public class PhormContractRunnerTests_Errors
    {
        class FailingDataReader : TestDbDataReader
        {
            public Exception ReadException { get; set; } = null!;

            public override bool Read()
            {
                throw ReadException;
            }
        }

        [TestMethod]
        public void Call__Error_not_processed_by_console__Thrown()
        {
            // Arrange
            var readException = new InvalidOperationException();
            var cmd = new TestDbCommand(new FailingDataReader
            {
                ReadException = readException
            });

            var conn = new TestPhormConnection("");
            conn.CommandQueue.Enqueue(cmd);

            var phorm = new TestPhormSession(conn)
            {
                ExceptionsAsConsoleMessage = true
            };

            // Act
            var ex = Assert.ThrowsException<InvalidOperationException>(() =>
            {
                _ = phorm.Call("Test", null);
            });

            // Assert
            Assert.AreSame(readException, ex);
        }

        [TestMethod]
        public void Call__Error_processed_by_console__Consumed()
        {
            // Arrange
            var readException = new InvalidOperationException();
            var cmd = new TestDbCommand(new FailingDataReader
            {
                ReadException = readException
            });

            var conn = new TestPhormConnection("");
            conn.CommandQueue.Enqueue(cmd);

            var phorm = new TestPhormSession(conn)
            {
                ConsoleMessageCaptureProvider = (s, g) => new TestConsoleMessageCapture(s, g)
                {
                    ProcessExceptionLogic = (e) => e == readException
                },
                ExceptionsAsConsoleMessage = true
            };

            // Act
            var res = phorm.Call("Test", null);

            // Assert
            Assert.AreEqual(1, res);
        }

        [TestMethod]
        public void Get__Error_not_processed_by_console__Thrown()
        {
            // Arrange
            var readException = new InvalidOperationException();
            var cmd = new TestDbCommand(new FailingDataReader
            {
                ReadException = readException
            });

            var conn = new TestPhormConnection("");
            conn.CommandQueue.Enqueue(cmd);

            var phorm = new TestPhormSession(conn)
            {
                ExceptionsAsConsoleMessage = true
            };

            // Act
            var ex = Assert.ThrowsException<InvalidOperationException>(() =>
            {
                _ = phorm.From("Test", null).Get<object>();
            });

            // Assert
            Assert.AreSame(readException, ex);
        }

        [TestMethod]
        public void Get__Error_processed_by_console__Consumed()
        {
            // Arrange
            var readException = new InvalidOperationException();
            var cmd = new TestDbCommand(new FailingDataReader
            {
                ReadException = readException
            });

            var conn = new TestPhormConnection("");
            conn.CommandQueue.Enqueue(cmd);

            var phorm = new TestPhormSession(conn)
            {
                ConsoleMessageCaptureProvider = (s, g) => new TestConsoleMessageCapture(s, g)
                {
                    ProcessExceptionLogic = (e) => e == readException
                },
                ExceptionsAsConsoleMessage = true
            };

            // Act
            var res = phorm.From("Test", null).Get<object>();

            // Assert
            Assert.IsNull(res);
        }
    }
}