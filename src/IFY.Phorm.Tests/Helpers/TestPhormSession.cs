using IFY.Phorm.Connectivity;
using IFY.Phorm.Execution;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.CodeAnalysis;

namespace IFY.Phorm.Tests
{
    [ExcludeFromCodeCoverage]
    internal partial class TestPhormSession : AbstractPhormSession
    {
        public TestPhormConnectionProvider? TestConnectionProvider => _connectionProvider as TestPhormConnectionProvider;

        public IReadOnlyList<IAsyncDbCommand> Commands => _commands.AsReadOnly();
        private readonly List<IAsyncDbCommand> _commands = new List<IAsyncDbCommand>();

        public Func<TestPhormSession, Guid, AbstractConsoleMessageCapture>? ConsoleMessageCaptureProvider { get; set; }
        public List<ConsoleMessage> ConsoleMessages { get; } = new List<ConsoleMessage>();

        public override bool SupportsTransactions => false;

        public override bool IsInTransaction => false;

        public TestPhormSession()
            : this(new TestPhormConnectionProvider())
        {
        }
        public TestPhormSession(IPhormDbConnectionProvider connectionProvider)
            : base(connectionProvider, null)
        {
        }

        public override ITransactedPhormSession BeginTransaction()
        {
            throw new NotSupportedException();
        }

        protected override IAsyncDbCommand CreateCommand(IPhormDbConnection connection, string schema, string objectName, DbObjectType objectType)
        {
            var cmd = base.CreateCommand(connection, schema, objectName, objectType);
            _commands.Add(cmd);
            return cmd;
        }

        protected override string? GetConnectionName() => null;

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
}
