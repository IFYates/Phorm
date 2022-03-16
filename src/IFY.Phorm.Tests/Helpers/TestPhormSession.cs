using IFY.Phorm.Connectivity;
using IFY.Phorm.Execution;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.CodeAnalysis;

namespace IFY.Phorm.Tests
{
    [ExcludeFromCodeCoverage]
    internal class TestPhormSession : AbstractPhormSession
    {
        public TestPhormConnectionProvider? TestConnectionProvider => _connectionProvider as TestPhormConnectionProvider;

        public List<IAsyncDbCommand> Commands { get; } = new List<IAsyncDbCommand>();

        public bool ProcessConsoleMessages { get; set; }
        public List<ConsoleMessage> ConsoleMessages { get; } = new List<ConsoleMessage>();

        public override bool SupportsTransactions => false;

        public override bool IsInTransaction => false;

        public TestPhormSession()
            : this(new TestPhormConnectionProvider())
        {
        }
        public TestPhormSession(IPhormDbConnectionProvider connectionProvider)
            : base(connectionProvider)
        {
        }

        public override ITransactedPhormSession BeginTransaction()
        {
            throw new NotSupportedException();
        }

        protected override IAsyncDbCommand CreateCommand(IPhormDbConnection connection, string schema, string objectName, DbObjectType objectType)
        {
            var cmd = base.CreateCommand(connection, schema, objectName, objectType);
            Commands.Add(cmd);
            return cmd;
        }

        protected override string? GetConnectionName() => null;

        class TestConsoleMessageCapture : AbstractConsoleMessageCapture
        {
            private readonly new TestPhormSession _session;

            public TestConsoleMessageCapture(TestPhormSession session, Guid commandGuid)
                : base(session, commandGuid)
            {
                _session = session;
                sendAllMessages();
            }

            private void sendAllMessages()
            {
                var messages = _session.ConsoleMessages.ToArray();
                _session.ConsoleMessages.Clear();
                foreach (var message in messages)
                {
                    OnConsoleMessage(new ConsoleMessage
                    {
                        Level = message.Level,
                        Source = message.Source,
                        Message = message.Message
                    });
                }
            }

            public override void Dispose()
            {
                sendAllMessages();
            }
        }
        protected internal override AbstractConsoleMessageCapture StartConsoleCapture(Guid commandGuid, IAsyncDbCommand cmd)
        {
            return ProcessConsoleMessages
                ? new TestConsoleMessageCapture(this, commandGuid)
                : base.StartConsoleCapture(commandGuid, cmd);
        }
    }
}
