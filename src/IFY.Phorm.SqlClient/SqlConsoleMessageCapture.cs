﻿using IFY.Phorm.Execution;
using Microsoft.Data.SqlClient;
using System;

namespace IFY.Phorm.SqlClient
{
    public class SqlConsoleMessageCapture : AbstractConsoleMessageCapture
    {
        private readonly SqlConnection _conn;

        public SqlConsoleMessageCapture(AbstractPhormSession session, Guid commandGuid, SqlConnection conn)
            : base(session, commandGuid)
        {
            _conn = conn;
            conn.InfoMessage += captureInfoMessage;
        }

        public override bool ProcessException(Exception ex)
        {
            if (ex is SqlException sqlException)
            {
                HasError = true;
                fromSqlErrors(sqlException.Errors, true);
                return true;
            }
            return false;
        }

        private void captureInfoMessage(object sender, SqlInfoMessageEventArgs e)
        {
            // TODO: possible only for cmd?
            fromSqlErrors(e.Errors, false);
        }

        private void fromSqlErrors(SqlErrorCollection errors, bool isException)
        {
            foreach (SqlError err in errors)
            {
                // TODO: other info
                OnConsoleMessage(new ConsoleMessage
                {
                    IsError = isException,
                    Level = err.Class,
                    Source = $"{err.Procedure} @ {err.LineNumber}",
                    Message = err.Message
                });
            }
        }

        public override void Dispose()
        {
            _conn.InfoMessage -= captureInfoMessage;
            GC.SuppressFinalize(this);
        }
    }
}
