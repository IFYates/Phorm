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
    public partial class TestDbCommand : IAsyncDbCommand
    {
        public int ReturnValue { get; set; } = 1;
        public DbDataReader Reader { get; set; }

        public virtual string CommandText { get; set; } = string.Empty;
        public virtual int CommandTimeout { get; set; }
        public virtual CommandType CommandType { get; set; }
        public virtual IDbConnection? Connection { get; set; }

        public virtual IDataParameterCollection Parameters { get; } = new TestParameterCollection();

        public virtual IDbTransaction? Transaction { get; set; }
        public virtual UpdateRowSource UpdatedRowSource { get; set; }

        public TestDbCommand()
        {
            Reader = new TestDbReader();
        }
        public TestDbCommand(DbDataReader reader)
        {
            Reader = reader;
        }

        public virtual void Cancel()
        {
        }

        public virtual IDbDataParameter CreateParameter()
        {
            return new TestDbParameter();
        }

        public virtual void Dispose()
        {
        }

        public virtual int ExecuteNonQuery()
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

        public virtual object? ExecuteScalar()
        {
            throw new NotImplementedException();
        }

        public virtual void Prepare()
        {
        }
    }
}
