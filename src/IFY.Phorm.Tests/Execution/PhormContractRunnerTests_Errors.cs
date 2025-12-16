using IFY.Phorm.Tests;

namespace IFY.Phorm.Execution.Tests;

[TestClass]
public class PhormContractRunnerTests_Errors
{
    public TestContext TestContext { get; set; }

    class FailingDataReader : TestDbDataReader
    {
        public Exception ReadException { get; set; } = null!;

        public override bool Read()
        {
            throw ReadException;
        }
    }

    [TestInitialize]
    public void Init()
    {
        AbstractPhormSession.ResetConnectionPool();
    }

    [TestMethod]
    public async Task Call__Error_not_processed_by_console__Thrown()
    {
        // Arrange
        var readException = new InvalidOperationException();
        var cmd = new TestDbCommand(new FailingDataReader
        {
            ReadException = readException
        });

        var conn = new TestPhormConnection();
        conn.CommandQueue.Enqueue(cmd);

        var phorm = new TestPhormSession(conn)
        {
            ExceptionsAsConsoleMessage = true
        };

        // Act
        var ex = await Assert.ThrowsExactlyAsync<InvalidOperationException>
            (async () => await phorm.CallAsync("Test", null, TestContext.CancellationToken));

        // Assert
        Assert.AreSame(readException, ex);
    }

    [TestMethod]
    public async Task Call__Error_processed_by_console__Consumed()
    {
        // Arrange
        var readException = new InvalidOperationException();
        var cmd = new TestDbCommand(new FailingDataReader
        {
            ReadException = readException
        });

        var conn = new TestPhormConnection();
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
        var res = await phorm.CallAsync("Test", null, TestContext.CancellationToken);

        // Assert
        Assert.AreEqual(1, res);
    }

    [TestMethod]
    public async Task Get__Error_not_processed_by_console__Thrown()
    {
        // Arrange
        var readException = new InvalidOperationException();
        var cmd = new TestDbCommand(new FailingDataReader
        {
            ReadException = readException
        });

        var conn = new TestPhormConnection();
        conn.CommandQueue.Enqueue(cmd);

        var phorm = new TestPhormSession(conn)
        {
            ExceptionsAsConsoleMessage = true
        };

        // Act
        var ex = await Assert.ThrowsExactlyAsync<InvalidOperationException>
            (async () => await phorm.From("Test", null).GetAsync<object>(TestContext.CancellationToken));

        // Assert
        Assert.AreSame(readException, ex);
    }

    [TestMethod]
    public async Task Get__Error_processed_by_console__Consumed()
    {
        // Arrange
        var readException = new InvalidOperationException();
        var cmd = new TestDbCommand(new FailingDataReader
        {
            ReadException = readException
        });

        var conn = new TestPhormConnection();
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
        var res = await phorm.From("Test", null).GetAsync<object>(TestContext.CancellationToken);

        // Assert
        Assert.IsNull(res);
    }
}