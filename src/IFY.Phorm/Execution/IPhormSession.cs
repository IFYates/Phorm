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
        /// The event invoked when a command is about to be executed.
        /// </summary>
        event EventHandler<CommandExecutingEventArgs>? CommandExecuting;

        /// <summary>
        /// The event invoked when a command has finished executing.
        /// </summary>
        event EventHandler<CommandExecutedEventArgs>? CommandExecuted;

        /// <summary>
        /// A result record contained a column not specified in the target entity type.
        /// </summary>
        event EventHandler<UnresolvedContractMemberEventArgs>? UnresolvedContractMember;

        /// <summary>
        /// A log message was received during execution.
        /// </summary>
        event EventHandler<ConsoleMessageEventArgs>? ConsoleMessage;

        #endregion Events

        /// <summary>
        /// Whether to throw an exception if an invocation result includes more records than expected.
        /// Defaults to true.
        /// </summary>
        /// <exception cref="System.InvalidOperationException"></exception>
        bool StrictResultSize { get; set; }

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
