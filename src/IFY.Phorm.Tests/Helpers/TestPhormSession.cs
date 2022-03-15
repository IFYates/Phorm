using IFY.Phorm.Connectivity;
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
    }
}
