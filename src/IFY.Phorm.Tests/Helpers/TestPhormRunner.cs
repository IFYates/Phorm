using IFY.Phorm.Connectivity;
using System;
using System.Collections.Generic;
using System.Data;

namespace IFY.Phorm.Tests
{
    internal class TestPhormRunner : AbstractPhormRunner
    {
        public TestPhormConnectionProvider? TestConnectionProvider => _connectionProvider as TestPhormConnectionProvider;

        public List<IAsyncDbCommand> Commands { get; } = new();

        public override bool SupportsTransactions => false;

        public override bool IsInTransaction => false;

        public TestPhormRunner()
            : this(new TestPhormConnectionProvider())
        {
        }
        public TestPhormRunner(IPhormDbConnectionProvider connectionProvider)
            : base(connectionProvider)
        {
        }

        public override ITransactedPhormRunner BeginTransaction()
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
