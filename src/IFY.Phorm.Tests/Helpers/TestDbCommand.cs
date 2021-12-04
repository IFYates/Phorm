using System;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace IFY.Phorm.Tests
{
    /// <summary>
    /// Mockable test object with useful default implementation.
    /// </summary>
    public partial class TestDbCommand : DbCommand, IAsyncDbCommand, IDbCommand
    {
        public int ReturnValue { get; set; } = 1;
        public DbDataReader Reader { get; set; }

        public override string CommandText { get; set; } = string.Empty;
        public override int CommandTimeout { get; set; }
        public override CommandType CommandType { get; set; }
        public virtual IDbConnection? Connection { get; set; }

        public virtual IDataParameterCollection Parameters { get; } = new TestParameterCollection();

        public virtual IDbTransaction? Transaction { get; set; }
        public override UpdateRowSource UpdatedRowSource { get; set; }

        protected override DbConnection? DbConnection { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        protected override DbParameterCollection DbParameterCollection => throw new NotImplementedException();
        protected override DbTransaction? DbTransaction { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public override bool DesignTimeVisible { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public TestDbCommand()
        {
            Reader = new TestDbReader();
        }
        public TestDbCommand(DbDataReader reader)
        {
            Reader = reader;
        }

        public override void Cancel()
        {
        }

        public virtual IDbDataParameter CreateParameter()
        {
            return new TestDbParameter();
        }

        public new virtual void Dispose()
        {
            base.Dispose();
        }

        public override int ExecuteNonQuery()
        {
            throw new NotImplementedException();
        }

        public virtual IDataReader ExecuteReader()
        {
            throw new NotImplementedException();
        }

        public virtual IDataReader ExecuteReader(CommandBehavior behavior)
        {
            throw new NotImplementedException();
        }

        public virtual Task<DbDataReader> ExecuteReaderAsync(CancellationToken cancellationToken)
        {
            var retvalParam = Parameters.AsParameters()
                .FirstOrDefault(p => p.Direction == ParameterDirection.ReturnValue);
            if (retvalParam != null)
            {
                retvalParam.Value = ReturnValue;
            }

            return Task.FromResult(Reader);
        }

        public override object? ExecuteScalar()
        {
            throw new NotImplementedException();
        }

        public override void Prepare()
        {
        }

        protected override DbParameter CreateDbParameter()
        {
            throw new NotImplementedException();
        }

        protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
        {
            throw new NotImplementedException();
        }
    }
}
