using IFY.Phorm.Connectivity;
using IFY.Phorm.Execution;
using System.Data;
using System.Diagnostics.CodeAnalysis;

namespace IFY.Phorm.Tests;

[ExcludeFromCodeCoverage]
internal partial class TestPhormSession : AbstractPhormSession
{
    public TestPhormConnection TestConnection { get; }

    public IReadOnlyList<IAsyncDbCommand> Commands => _commands.AsReadOnly();
    private readonly List<IAsyncDbCommand> _commands = [];

    public Func<TestPhormSession, Guid, AbstractConsoleMessageCapture>? ConsoleMessageCaptureProvider { get; set; }
    public List<ConsoleMessage> ConsoleMessages { get; } = [];

    public override bool SupportsTransactions => false;

    public override bool IsInTransaction => false;

    public bool IsReadOnly { get; private set; }

    public TestPhormSession(string? connectionName = null)
        : base(null!, connectionName ?? Guid.NewGuid().ToString())
    {
        TestConnection = new TestPhormConnection(connectionName);
    }
    public TestPhormSession(TestPhormConnection connection)
        : base(null!, connection.ConnectionName)
    {
        TestConnection = connection;
    }

    public override ITransactedPhormSession BeginTransaction()
    {
        throw new NotSupportedException();
    }

    protected internal override IAsyncDbCommand CreateCommand(IPhormDbConnection connection, string schema, string objectName, DbObjectType objectType)
    {
        var cmd = base.CreateCommand(connection, schema, objectName, objectType);
        _commands.Add(cmd);
        return cmd;
    }

    protected override IPhormDbConnection CreateConnection(bool readOnly)
    {
        IsReadOnly = readOnly;
        return TestConnection;
    }

    [ExcludeFromCodeCoverage]
    public override IPhormSession SetConnectionName(string connectionName)
    {
        throw new NotImplementedException();
    }

    protected internal override AbstractConsoleMessageCapture StartConsoleCapture(Guid commandGuid, IAsyncDbCommand cmd)
    {
        return ConsoleMessageCaptureProvider?.Invoke(this, commandGuid)
            ?? base.StartConsoleCapture(commandGuid, cmd);
    }
}
