using IFY.Phorm.Data;
using IFY.Phorm.EventArgs;
using IFY.Phorm.Tests;

namespace IFY.Phorm.Execution.Tests;

[TestClass]
public class PhormContractRunnerTests_Events
{
    public TestContext TestContext { get; set; }

    private int _unwantedInvocations = 0;
    private Action<object?, CommandExecutingEventArgs> _globalCommandExecuting = null!;
    private void invokeHandler(object? sender, CommandExecutingEventArgs args) => _globalCommandExecuting?.Invoke(sender, args);
    private Action<object?, CommandExecutedEventArgs> _globalCommandExecuted = null!;
    private void invokeHandler(object? sender, CommandExecutedEventArgs args) => _globalCommandExecuted?.Invoke(sender, args);
    private Action<object?, UnexpectedRecordColumnEventArgs> _globalUnexpectedRecordColumn = null!;
    private void invokeHandler(object? sender, UnexpectedRecordColumnEventArgs args) => _globalUnexpectedRecordColumn?.Invoke(sender, args);
    private Action<object?, UnresolvedContractMemberEventArgs> _globalUnresolvedContractMember = null!;
    private void invokeHandler(object? sender, UnresolvedContractMemberEventArgs args) => _globalUnresolvedContractMember?.Invoke(sender, args);
    private Action<object?, ConsoleMessageEventArgs> _globalConsoleMessage = null!;
    private void invokeHandler(object? sender, ConsoleMessageEventArgs args) => _globalConsoleMessage?.Invoke(sender, args);

    public class TestEntity
    {
        public string Getter { get; } = string.Empty;
        public string GetterSetter { get; set; } = string.Empty;
        public string Setter { private get; set; } = string.Empty;
    }

    [TestInitialize]
    public void Init()
    {
        AbstractPhormSession.ResetConnectionPool();

        Events.CommandExecuting += invokeHandler;
        Events.CommandExecuted += invokeHandler;
        Events.UnexpectedRecordColumn += invokeHandler;
        Events.UnresolvedContractMember += invokeHandler;
        Events.ConsoleMessage += invokeHandler;
    }
    [TestCleanup]
    public void Clean()
    {
        Events.CommandExecuting -= invokeHandler;
        Events.CommandExecuted -= invokeHandler;
        Events.UnexpectedRecordColumn -= invokeHandler;
        Events.UnresolvedContractMember -= invokeHandler;
        Events.ConsoleMessage -= invokeHandler;
    }

    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    private void eventInvokeFail(object? sender, System.EventArgs args)
    {
        ++_unwantedInvocations;
        Assert.Fail();
    }

    [TestMethod]
    public async Task Call__Invokes_both_CommandExecuting_events_before_executing()
    {
        // Arrange
        var conn = new TestPhormConnection("")
        {
            DefaultSchema = "schema"
        };

        var cmd = new TestDbCommand
        {
            OnExecuteReaderAsync = () => throw new NotImplementedException()
        };
        conn.CommandQueue.Enqueue(cmd);

        var phorm = new TestPhormSession(conn);

        (object? sender, CommandExecutingEventArgs args)? instanceEvent = null;
        phorm.CommandExecuting += (sender, args) =>
        {
            instanceEvent = (sender, args);
        };

        (object? sender, CommandExecutingEventArgs args)? globalEvent = null;
        _globalCommandExecuting = (sender, args) =>
        {
            globalEvent = (sender, args);
        };

        phorm.CommandExecuted += eventInvokeFail;
        _globalCommandExecuted = eventInvokeFail;

        var runner = new PhormContractRunner<IPhormContract>(phorm, "CallTest", DbObjectType.StoredProcedure,
            new { Arg1 = 1, Arg2 = "2" }, null);

        // Act
        var ex = await Assert.ThrowsExactlyAsync<NotImplementedException>
            (async () => await runner.CallAsync(TestContext.CancellationToken));

        // Assert
        Assert.AreEqual(0, _unwantedInvocations);
        Assert.AreSame(phorm, instanceEvent!.Value.sender);
        Assert.AreEqual("[schema].[usp_CallTest]", instanceEvent.Value.args.CommandText);
        Assert.HasCount(3, instanceEvent.Value.args.CommandParameters); // + return
        Assert.AreEqual(1, (int)instanceEvent.Value.args.CommandParameters["@Arg1"]!);
        Assert.AreEqual("2", (string)instanceEvent.Value.args.CommandParameters["@Arg2"]!);
        Assert.AreSame(phorm, globalEvent!.Value.sender);
        Assert.AreSame(instanceEvent.Value.args, globalEvent.Value.args);
    }

    [TestMethod]
    public async Task Call__Invokes_both_CommandExecuted_events_after_execution()
    {
        // Arrange
        var conn = new TestPhormConnection("")
        {
            DefaultSchema = "schema"
        };

        var cmd = new TestDbCommand
        {
            ReturnValue = DateTime.UtcNow.Millisecond
        };
        conn.CommandQueue.Enqueue(cmd);

        var phorm = new TestPhormSession(conn);

        (object? sender, CommandExecutedEventArgs args)? instanceEvent = null;
        phorm.CommandExecuted += (sender, args) =>
        {
            instanceEvent = (sender, args);
        };

        (object? sender, CommandExecutedEventArgs args)? globalEvent = null;
        _globalCommandExecuted = (sender, args) =>
        {
            globalEvent = (sender, args);
        };

        Guid? commandGuid = null;
        phorm.CommandExecuting += (_, a) =>
        {
            Assert.AreEqual(commandGuid ??= a.CommandGuid, a.CommandGuid);
            Assert.IsNull(instanceEvent);
            Assert.IsNull(globalEvent);
        };
        _globalCommandExecuting = (_, a) =>
        {
            Assert.AreEqual(commandGuid ??= a.CommandGuid, a.CommandGuid);
            Assert.IsNull(instanceEvent);
            Assert.IsNull(globalEvent);
        };

        var runner = new PhormContractRunner<IPhormContract>(phorm, "CallTest", DbObjectType.StoredProcedure,
            new { Arg1 = 1, Arg2 = "2" }, null);

        // Act
        var res = await runner.CallAsync(TestContext.CancellationToken);

        // Assert
        Assert.AreSame(phorm, instanceEvent!.Value.sender);
        Assert.AreEqual(commandGuid, instanceEvent.Value.args.CommandGuid);
        Assert.AreEqual("[schema].[usp_CallTest]", instanceEvent.Value.args.CommandText);
        Assert.HasCount(3, instanceEvent.Value.args.CommandParameters); // + return
        Assert.AreEqual(1, (int)instanceEvent.Value.args.CommandParameters["@Arg1"]!);
        Assert.AreEqual("2", (string)instanceEvent.Value.args.CommandParameters["@Arg2"]!);
        Assert.IsFalse(instanceEvent.Value.args.ResultCount.HasValue);
        Assert.AreEqual(res, instanceEvent.Value.args.ReturnValue);
        Assert.AreSame(phorm, globalEvent!.Value.sender);
        Assert.AreSame(instanceEvent.Value.args, globalEvent.Value.args);
    }

    [TestMethod]
    public async Task Get__Invokes_both_CommandExecuting_events_before_executing()
    {
        // Arrange
        var conn = new TestPhormConnection("")
        {
            DefaultSchema = "schema"
        };

        var cmd = new TestDbCommand
        {
            OnExecuteReaderAsync = () => throw new NotImplementedException()
        };
        conn.CommandQueue.Enqueue(cmd);

        var phorm = new TestPhormSession(conn);

        (object? sender, CommandExecutingEventArgs args)? instanceEvent = null;
        phorm.CommandExecuting += (sender, args) =>
        {
            instanceEvent = (sender, args);
        };

        (object? sender, CommandExecutingEventArgs args)? globalEvent = null;
        _globalCommandExecuting = (sender, args) =>
        {
            globalEvent = (sender, args);
        };

        phorm.CommandExecuted += eventInvokeFail;
        _globalCommandExecuted = eventInvokeFail;

        var runner = new PhormContractRunner<IPhormContract>(phorm, "CallTest", DbObjectType.StoredProcedure,
            new { Arg1 = 1, Arg2 = "2" }, null);

        // Act
        await Assert.ThrowsExactlyAsync<NotImplementedException>
            (async () => await runner.GetAsync<object>(TestContext.CancellationToken));

        // Assert
        Assert.AreEqual(0, _unwantedInvocations);
        Assert.AreSame(phorm, instanceEvent!.Value.sender);
        Assert.AreEqual("[schema].[usp_CallTest]", instanceEvent.Value.args.CommandText);
        Assert.HasCount(3, instanceEvent.Value.args.CommandParameters); // + return
        Assert.AreEqual(1, (int)instanceEvent.Value.args.CommandParameters["@Arg1"]!);
        Assert.AreEqual("2", (string)instanceEvent.Value.args.CommandParameters["@Arg2"]!);
        Assert.AreSame(phorm, globalEvent!.Value.sender);
        Assert.AreSame(instanceEvent.Value.args, globalEvent.Value.args);
    }

    [TestMethod]
    public async Task Get__Invokes_both_CommandExecuted_events_after_execution()
    {
        // Arrange
        var conn = new TestPhormConnection("")
        {
            DefaultSchema = "schema",
        };

        var cmd = new TestDbCommand(new TestDbDataReader
        {
            Data =
            [
                [],
                [],
                [],
                [],
                [],
            ]
        })
        {
            ReturnValue = DateTime.UtcNow.Millisecond
        };
        conn.CommandQueue.Enqueue(cmd);

        var phorm = new TestPhormSession(conn);

        (object? sender, CommandExecutedEventArgs args)? instanceEvent = null;
        phorm.CommandExecuted += (sender, args) =>
        {
            instanceEvent = (sender, args);
        };

        (object? sender, CommandExecutedEventArgs args)? globalEvent = null;
        _globalCommandExecuted = (sender, args) =>
        {
            globalEvent = (sender, args);
        };

        Guid? commandGuid = null;
        phorm.CommandExecuting += (_, a) =>
        {
            Assert.AreEqual(commandGuid ??= a.CommandGuid, a.CommandGuid);
            Assert.IsNull(instanceEvent);
            Assert.IsNull(globalEvent);
        };
        _globalCommandExecuting = (_, a) =>
        {
            Assert.AreEqual(commandGuid ??= a.CommandGuid, a.CommandGuid);
            Assert.IsNull(instanceEvent);
            Assert.IsNull(globalEvent);
        };

        var runner = new PhormContractRunner<IPhormContract>(phorm, "CallTest", DbObjectType.StoredProcedure,
            new { Arg1 = 1, Arg2 = "2" }, null);

        // Act
        var res = await runner.GetAsync<object[]>(TestContext.CancellationToken);

        // Assert
        Assert.AreSame(phorm, instanceEvent!.Value.sender);
        Assert.AreEqual(commandGuid, instanceEvent.Value.args.CommandGuid);
        Assert.AreEqual("[schema].[usp_CallTest]", instanceEvent.Value.args.CommandText);
        Assert.HasCount(3, instanceEvent.Value.args.CommandParameters); // + return
        Assert.AreEqual(1, (int)instanceEvent.Value.args.CommandParameters["@Arg1"]!);
        Assert.AreEqual("2", (string)instanceEvent.Value.args.CommandParameters["@Arg2"]!);
        Assert.AreEqual(res!.Length, instanceEvent.Value.args.ResultCount);
        Assert.AreEqual(cmd.ReturnValue, instanceEvent.Value.args.ReturnValue);
        Assert.AreSame(phorm, globalEvent!.Value.sender);
        Assert.AreSame(instanceEvent.Value.args, globalEvent.Value.args);
    }

    [TestMethod]
    public async Task Get__Unmapped_record_column__Invokes_both_UnexpectedRecordColumn_events()
    {
        // Arrange
        var conn = new TestPhormConnection("")
        {
            DefaultSchema = "schema",
        };

        var cmd = new TestDbCommand(new TestDbDataReader
        {
            Data =
            [
                new()
                {
                    ["Column"] = "data"
                }
            ]
        })
        {
            ReturnValue = DateTime.UtcNow.Millisecond
        };
        conn.CommandQueue.Enqueue(cmd);

        var phorm = new TestPhormSession(conn);

        (object? sender, UnexpectedRecordColumnEventArgs args)? instanceEvent = null;
        phorm.UnexpectedRecordColumn += (sender, args) =>
        {
            instanceEvent = (sender, args);
        };

        (object? sender, UnexpectedRecordColumnEventArgs args)? globalEvent = null;
        _globalUnexpectedRecordColumn = (sender, args) =>
        {
            globalEvent = (sender, args);
        };

        Guid? commandGuid = null;
        phorm.CommandExecuting += (_, a) =>
        {
            Assert.AreEqual(commandGuid ??= a.CommandGuid, a.CommandGuid);
            Assert.IsNull(instanceEvent);
            Assert.IsNull(globalEvent);
        };
        _globalCommandExecuting = (_, a) =>
        {
            Assert.AreEqual(commandGuid ??= a.CommandGuid, a.CommandGuid);
            Assert.IsNull(instanceEvent);
            Assert.IsNull(globalEvent);
        };

        var runner = new PhormContractRunner<IPhormContract>(phorm, "CallTest", DbObjectType.StoredProcedure,
            new { Arg1 = 1, Arg2 = "2" }, null);

        // Act
        await runner.GetAsync<TestEntity[]>(TestContext.CancellationToken);

        // Assert
        Assert.AreSame(phorm, instanceEvent!.Value.sender);
        Assert.AreEqual(commandGuid, instanceEvent.Value.args.CommandGuid);
        Assert.AreEqual(typeof(TestEntity), instanceEvent.Value.args.EntityType);
        Assert.AreEqual("Column", instanceEvent.Value.args.ColumnName);
        Assert.AreSame(phorm, globalEvent!.Value.sender);
        Assert.AreSame(instanceEvent.Value.args, globalEvent!.Value.args);
    }

    [TestMethod]
    public async Task Get__Unused_entity_member__Invokes_both_UnresolvedContractMember_events()
    {
        // Arrange
        var conn = new TestPhormConnection("")
        {
            DefaultSchema = "schema",
        };

        var cmd = new TestDbCommand(new TestDbDataReader
        {
            Data =
            [
                []
            ]
        })
        {
            ReturnValue = DateTime.UtcNow.Millisecond
        };
        conn.CommandQueue.Enqueue(cmd);

        var phorm = new TestPhormSession(conn);

        (object? sender, UnresolvedContractMemberEventArgs args)? instanceEvent = null;
        phorm.UnresolvedContractMember += (sender, args) =>
        {
            instanceEvent = (sender, args);
        };

        (object? sender, UnresolvedContractMemberEventArgs args)? globalEvent = null;
        _globalUnresolvedContractMember = (sender, args) =>
        {
            globalEvent = (sender, args);
        };

        Guid? commandGuid = null;
        phorm.CommandExecuting += (_, a) =>
        {
            Assert.AreEqual(commandGuid ??= a.CommandGuid, a.CommandGuid);
            Assert.IsNull(instanceEvent);
            Assert.IsNull(globalEvent);
        };
        _globalCommandExecuting = (_, a) =>
        {
            Assert.AreEqual(commandGuid ??= a.CommandGuid, a.CommandGuid);
            Assert.IsNull(instanceEvent);
            Assert.IsNull(globalEvent);
        };

        var runner = new PhormContractRunner<IPhormContract>(phorm, "CallTest", DbObjectType.StoredProcedure,
            new { Arg1 = 1, Arg2 = "2" }, null);

        // Act
        await runner.GetAsync<TestEntity>(TestContext.CancellationToken);

        // Assert
        Assert.AreSame(phorm, instanceEvent!.Value.sender);
        Assert.AreEqual(commandGuid, instanceEvent.Value.args.CommandGuid);
        Assert.AreEqual(typeof(TestEntity), instanceEvent.Value.args.EntityType);
        Assert.HasCount(2, instanceEvent.Value.args.MemberNames);
        Assert.AreEqual("GetterSetter", instanceEvent.Value.args.MemberNames[0]);
        Assert.AreEqual("Setter", instanceEvent.Value.args.MemberNames[1]);
        Assert.AreSame(phorm, globalEvent!.Value.sender);
        Assert.AreSame(instanceEvent.Value.args, globalEvent!.Value.args);
    }

    [TestMethod]
    [DataRow(false, DisplayName = "Instance")]
    [DataRow(true, DisplayName = "Global")]
    public async Task Call__Events_can_receive_console_messages(bool asGlobal)
    {
        // Arrange
        var conn = new TestPhormConnection("")
        {
            DefaultSchema = "schema"
        };

        var cmd = new TestDbCommand();
        conn.CommandQueue.Enqueue(cmd);

        var phorm = new TestPhormSession(conn)
        {
            ConsoleMessageCaptureProvider = (s, g) => new TestConsoleMessageCapture(s, g)
        };

        var consoleMessages = new List<ConsoleMessage>();
        if (asGlobal)
        {
            _globalConsoleMessage = (_, a) => consoleMessages.Add(a.ConsoleMessage);
        }
        else
        {
            phorm.ConsoleMessage += (_, a) => consoleMessages.Add(a.ConsoleMessage);
        }

        phorm.ConsoleMessages.Add(new ConsoleMessage { Message = "Message1" });
        phorm.ConsoleMessages.Add(new ConsoleMessage { Message = "Message2" });
        phorm.ConsoleMessages.Add(new ConsoleMessage { Message = "Message3" });

        var runner = new PhormContractRunner<IPhormContract>(phorm, "Test", DbObjectType.Default, null, null);

        // Act
        var res = await runner.CallAsync(TestContext.CancellationToken);

        // Assert
        Assert.AreEqual(1, res);
        Assert.HasCount(3, consoleMessages);
        Assert.AreEqual("Message1", consoleMessages[0].Message);
        Assert.AreEqual("Message2", consoleMessages[1].Message);
        Assert.AreEqual("Message3", consoleMessages[2].Message);
    }

    [TestMethod]
    [DataRow(false, DisplayName = "Instance")]
    [DataRow(true, DisplayName = "Global")]
    public async Task Get__Events_can_receive_console_messages(bool asGlobal)
    {
        // Arrange
        var conn = new TestPhormConnection("")
        {
            DefaultSchema = "schema"
        };

        var cmd = new TestDbCommand();
        conn.CommandQueue.Enqueue(cmd);

        var phorm = new TestPhormSession(conn)
        {
            ConsoleMessageCaptureProvider = (s, g) => new TestConsoleMessageCapture(s, g)
        };

        var consoleMessages = new List<ConsoleMessage>();
        if (asGlobal)
        {
            _globalConsoleMessage = (_, a) => consoleMessages.Add(a.ConsoleMessage);
        }
        else
        {
            phorm.ConsoleMessage += (_, a) => consoleMessages.Add(a.ConsoleMessage);
        }

        phorm.ConsoleMessages.Add(new ConsoleMessage { Message = "Message1" });
        phorm.ConsoleMessages.Add(new ConsoleMessage { Message = "Message2" });
        phorm.ConsoleMessages.Add(new ConsoleMessage { Message = "Message3" });

        var runner = new PhormContractRunner<IPhormContract>(phorm, "Test", DbObjectType.Default, null, null);

        // Act
        await runner.GetAsync<object>(TestContext.CancellationToken);

        // Assert
        Assert.HasCount(3, consoleMessages);
        Assert.AreEqual("Message1", consoleMessages[0].Message);
        Assert.AreEqual("Message2", consoleMessages[1].Message);
        Assert.AreEqual("Message3", consoleMessages[2].Message);
    }
}