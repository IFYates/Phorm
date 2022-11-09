using IFY.Phorm.Connectivity;
using IFY.Phorm.Data;
using IFY.Phorm.EventArgs;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace IFY.Phorm
{
    public interface IPhormSession
    {
        #region Events

        /// <summary>
        /// The event invoked when a new database connection is created.
        /// </summary>
        event EventHandler<ConnectedEventArgs> Connected;

        /// <summary>
        /// The event invoked when a command is about to be executed.
        /// </summary>
        event EventHandler<CommandExecutingEventArgs> CommandExecuting;

        /// <summary>
        /// The event invoked when a command has finished executing.
        /// </summary>
        event EventHandler<CommandExecutedEventArgs> CommandExecuted;

        /// <summary>
        /// A result record contained a column not specified in the target entity type.
        /// </summary>
        event EventHandler<UnexpectedRecordColumnEventArgs> UnexpectedRecordColumn;

        /// <summary>
        /// A result record did not contain a column specified in the target entity type.
        /// </summary>
        event EventHandler<UnresolvedContractMemberEventArgs> UnresolvedContractMember;

        /// <summary>
        /// A log message was received during execution.
        /// </summary>
        event EventHandler<ConsoleMessageEventArgs> ConsoleMessage;

        #endregion Events

        /// <summary>
        /// The connection name this session uses for database scoping.
        /// </summary>
        string? ConnectionName { get; }

        /// <summary>
        /// If true, will consume execution errors and treat like a console message.
        /// Defaults to value in <see cref="GlobalSettings.ExceptionsAsConsoleMessage"/>.
        /// </summary>
        bool ExceptionsAsConsoleMessage { get; set; }

        /// <summary>
        /// Whether to throw a <see cref="System.InvalidOperationException"/> if an invocation result includes more records than expected.
        /// Defaults to value in <see cref="GlobalSettings.StrictResultSize"/>.
        /// </summary>
        bool StrictResultSize { get; set; }

        /// <summary>
        /// Get a new instance of this session scoped with a different connection name.
        /// </summary>
        /// <param name="connectionName">The connection name to use when scoping the new session instance.</param>
        /// <returns>A new instance of this session with a different connection name.</returns>
        IPhormSession SetConnectionName(string connectionName);

        #region Call/get from action contract

        int Call(string contractName, object? args = null);
        int Call<TActionContract>(object? args = null)
            where TActionContract : IPhormContract;
        int Call<TActionContract>(TActionContract contract) // Same as "object? args = null", but allows better Intellisense
            where TActionContract : IPhormContract;

        Task<int> CallAsync(string contractName, object? args = null, CancellationToken? cancellationToken = null);
        Task<int> CallAsync<TActionContract>(object? args = null, CancellationToken? cancellationToken = null)
            where TActionContract : IPhormContract;
        Task<int> CallAsync<TActionContract>(TActionContract contract, CancellationToken? cancellationToken = null) // Same as "object? args = null", but allows better Intellisense
            where TActionContract : IPhormContract;

        IPhormContractRunner From(string contractName, object? args = null);
        IPhormContractRunner<TActionContract> From<TActionContract>(object? args = null)
            where TActionContract : IPhormContract;
        IPhormContractRunner<TActionContract> From<TActionContract>(TActionContract contract) // Same as "object? args = null", but allows better Intellisense
            where TActionContract : IPhormContract;

        #endregion Call/get from action contract

        #region Get from Table/View

        TResult? Get<TResult>(object? args = null)
            where TResult : class;
        TResult? Get<TResult>(TResult args) // Same as "object? args = null", but allows better Intellisense
            where TResult : class;

        Task<TResult?> GetAsync<TResult>(object? args = null, CancellationToken? cancellationToken = null)
            where TResult : class;
        Task<TResult?> GetAsync<TResult>(TResult args, CancellationToken? cancellationToken = null) // Same as "object? args = null", but allows better Intellisense
            where TResult : class;

        #endregion Get from Table/View

        #region Transactions

        /// <summary>
        /// True if this runner implementation supports transactions.
        /// </summary>
        bool SupportsTransactions { get; }

        /// <summary>
        /// True if this runner is currently in a transaction.
        /// </summary>
        bool IsInTransaction { get; }

        /// <summary>
        /// Begin a new transaction, with associated runner.
        /// </summary>
        /// <returns>The runner of the transaction.</returns>
        ITransactedPhormSession BeginTransaction();

        #endregion Transactions
    }
}
