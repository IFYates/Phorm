using IFY.Phorm.EventArgs;
using IFY.Phorm.Tests;

namespace IFY.Phorm.Execution.Tests;

[TestClass]
public class AbstractPhormSessionTests_Events
{
    private Action<object?, ConnectedEventArgs> _globalConnected = null!;
    private void invokeHandler(object? sender, ConnectedEventArgs args) => _globalConnected?.Invoke(sender, args);
    private Action<object?, CommandExecutingEventArgs> _globalCommandExecuting = null!;
    private void invokeHandler(object? sender, CommandExecutingEventArgs args) => _globalCommandExecuting?.Invoke(sender, args);
    private Action<object?, CommandExecutedEventArgs> _globalCommandExecuted = null!;
    private void invokeHandler(object? sender, CommandExecutedEventArgs args) => _globalCommandExecuted?.Invoke(sender, args);
    private Action<object?, UnexpectedRecordColumnEventArgs> _globalUnexpectedRecordColumn = null!;
    private void invokeHandler(object? sender, UnexpectedRecordColumnEventArgs args) => _globalUnexpectedRecordColumn?.Invoke(sender, args);
    private Action<object?, UnresolvedContractMemberEventArgs> _globalUnresolvedContractMember = null!;
    private void invokeHandler(object? sender, UnresolvedContractMemberEventArgs args) => _globalUnresolvedContractMember?.Invoke(sender, args);

    [TestInitialize]
    public void Init()
    {
        Events.Connected += invokeHandler;
        Events.CommandExecuting += invokeHandler;
        Events.CommandExecuted += invokeHandler;
        Events.UnexpectedRecordColumn += invokeHandler;
        Events.UnresolvedContractMember += invokeHandler;
    }
    [TestCleanup]
    public void Clean()
    {
        Events.Connected -= invokeHandler;
        Events.CommandExecuting -= invokeHandler;
        Events.CommandExecuted -= invokeHandler;
        Events.UnexpectedRecordColumn -= invokeHandler;
        Events.UnresolvedContractMember -= invokeHandler;
    }

    [TestMethod]
    [DataRow(false, DisplayName = "Instance")]
    [DataRow(true, DisplayName = "Global")]
    public void OnConnected__Ignores_exceptions(bool isGlobal)
    {
        // Arrange
        var phorm = new TestPhormSession();

        var wasCalled = false;
        if (isGlobal)
        {
            _globalConnected = (_, __) =>
            {
                wasCalled = true;
                throw new Exception();
            };
        }
        else
        {
            phorm.Connected += (_, __) =>
            {
                wasCalled = true;
                throw new Exception();
            };
        }

        // Act
        phorm.OnConnected(new ConnectedEventArgs());

        // Assert
        Assert.IsTrue(wasCalled);
    }

    [TestMethod]
    [DataRow(false, DisplayName = "Instance")]
    [DataRow(true, DisplayName = "Global")]
    public void OnCommandExecuting__Ignores_exceptions(bool isGlobal)
    {
        // Arrange
        var phorm = new TestPhormSession();

        var wasCalled = false;
        if (isGlobal)
        {
            _globalCommandExecuting = (_, __) =>
            {
                wasCalled = true;
                throw new Exception();
            };
        }
        else
        {
            phorm.CommandExecuting += (_, __) =>
            {
                wasCalled = true;
                throw new Exception();
            };
        }

        // Act
        phorm.OnCommandExecuting(new CommandExecutingEventArgs());

        // Assert
        Assert.IsTrue(wasCalled);
    }

    [TestMethod]
    [DataRow(false, DisplayName = "Instance")]
    [DataRow(true, DisplayName = "Global")]
    public void OnCommandExecuted__Ignores_exceptions(bool isGlobal)
    {
        // Arrange
        var phorm = new TestPhormSession();

        var wasCalled = false;
        if (isGlobal)
        {
            _globalCommandExecuted = (_, __) =>
            {
                wasCalled = true;
                throw new Exception();
            };
        }
        else
        {
            phorm.CommandExecuted += (_, __) =>
            {
                wasCalled = true;
                throw new Exception();
            };
        }

        // Act
        phorm.OnCommandExecuted(new CommandExecutedEventArgs());

        // Assert
        Assert.IsTrue(wasCalled);
    }

    [TestMethod]
    [DataRow(false, DisplayName = "Instance")]
    [DataRow(true, DisplayName = "Global")]
    public void OnUnexpectedRecordColumn__Ignores_exceptions(bool isGlobal)
    {
        // Arrange
        var phorm = new TestPhormSession();

        var wasCalled = false;
        if (isGlobal)
        {
            _globalUnexpectedRecordColumn = (_, __) =>
            {
                wasCalled = true;
                throw new Exception();
            };
        }
        else
        {
            phorm.UnexpectedRecordColumn += (_, __) =>
            {
                wasCalled = true;
                throw new Exception();
            };
        }

        // Act
        phorm.OnUnexpectedRecordColumn(new UnexpectedRecordColumnEventArgs());

        // Assert
        Assert.IsTrue(wasCalled);
    }

    [TestMethod]
    [DataRow(false, DisplayName = "Instance")]
    [DataRow(true, DisplayName = "Global")]
    public void OnUnresolvedContractMember__Ignores_exceptions(bool isGlobal)
    {
        // Arrange
        var phorm = new TestPhormSession();

        var wasCalled = false;
        if (isGlobal)
        {
            _globalUnresolvedContractMember = (_, __) =>
            {
                wasCalled = true;
                throw new Exception();
            };
        }
        else
        {
            phorm.UnresolvedContractMember += (_, __) =>
            {
                wasCalled = true;
                throw new Exception();
            };
        }

        // Act
        phorm.OnUnresolvedContractMember(new UnresolvedContractMemberEventArgs());

        // Assert
        Assert.IsTrue(wasCalled);
    }
}