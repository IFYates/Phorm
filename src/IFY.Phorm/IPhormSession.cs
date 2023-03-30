using IFY.Phorm.Data;
using IFY.Phorm.EventArgs;
using IFY.Phorm.Execution;

namespace IFY.Phorm;

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
    /// Whether to throw a <see cref="InvalidOperationException"/> if an invocation result includes more records than expected.
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

    public int Call(string contractName)
        => Call(contractName, args: null);
    int Call(string contractName, object? args);

    public int Call<TActionContract>()
        where TActionContract : IPhormContract
        => Call<TActionContract>(args: null);
    public int Call<TActionContract>(TActionContract contract) // Same as "object? args = null", but allows better Intellisense
        where TActionContract : IPhormContract
        => Call<TActionContract>(args: contract);
    int Call<TActionContract>(object? args)
        where TActionContract : IPhormContract;

    public Task<int> CallAsync(string contractName)
        => CallAsync(contractName, args: null, CancellationToken.None);
    public Task<int> CallAsync(string contractName, object? args)
        => CallAsync(contractName, args, CancellationToken.None);
    public Task<int> CallAsync(string contractName, CancellationToken cancellationToken)
        => CallAsync(contractName, args: null, cancellationToken);
    Task<int> CallAsync(string contractName, object? args, CancellationToken cancellationToken);

    public Task<int> CallAsync<TActionContract>()
        where TActionContract : IPhormContract
        => CallAsync<TActionContract>(args: null, CancellationToken.None);
    public Task<int> CallAsync<TActionContract>(object? args)
        where TActionContract : IPhormContract
        => CallAsync<TActionContract>(args, CancellationToken.None);
    public Task<int> CallAsync<TActionContract>(CancellationToken cancellationToken)
        where TActionContract : IPhormContract
        => CallAsync<TActionContract>(args: null, cancellationToken);
    public Task<int> CallAsync<TActionContract>(TActionContract contract) // Same as "object? args = null", but allows better Intellisense
        where TActionContract : IPhormContract
        => CallAsync<TActionContract>(args: contract, CancellationToken.None);
    public Task<int> CallAsync<TActionContract>(TActionContract contract, CancellationToken cancellationToken) // Same as "object? args = null", but allows better Intellisense
        where TActionContract : IPhormContract
        => CallAsync<TActionContract>(args: contract, cancellationToken);
    Task<int> CallAsync<TActionContract>(object? args, CancellationToken cancellationToken)
        where TActionContract : IPhormContract;

    public IPhormContractRunner From(string contractName)
        => From(contractName, args: null);
     IPhormContractRunner From(string contractName, object? args);

   public IPhormContractRunner<TActionContract> From<TActionContract>()
        where TActionContract : IPhormContract
        => From<TActionContract>(args: null);
    IPhormContractRunner<TActionContract> From<TActionContract>(object? args)
        where TActionContract : IPhormContract;
    public IPhormContractRunner<TActionContract> From<TActionContract>(TActionContract contract) // Same as "object? args = null", but allows better Intellisense
        where TActionContract : IPhormContract
        => From<TActionContract>(args: contract);

    #endregion Call/get from action contract

    #region Get from Table/View

    public TResult? Get<TResult>()
        where TResult : class
        => Get<TResult>(args: null);
    public TResult? Get<TResult>(TResult contract) // Same as "object? args = null", but allows better Intellisense
        where TResult : class
        => Get<TResult>(args: contract);
    TResult? Get<TResult>(object? args)
        where TResult : class;

    public Task<TResult?> GetAsync<TResult>()
        where TResult : class
        => GetAsync<TResult>(args: null, CancellationToken.None);
    public Task<TResult?> GetAsync<TResult>(object? args)
        where TResult : class
        => GetAsync<TResult>(args, CancellationToken.None);
    public Task<TResult?> GetAsync<TResult>(CancellationToken cancellationToken)
        where TResult : class
        => GetAsync<TResult>(args: null, cancellationToken);
    public Task<TResult?> GetAsync<TResult>(TResult contract) // Same as "object? args = null", but allows better Intellisense
        where TResult : class
        => GetAsync<TResult>(args: contract, CancellationToken.None);
    public Task<TResult?> GetAsync<TResult>(TResult contract, CancellationToken cancellationToken) // Same as "object? args = null", but allows better Intellisense
        where TResult : class
        => GetAsync<TResult>(args: contract, cancellationToken);
    Task<TResult?> GetAsync<TResult>(object? args, CancellationToken cancellationToken)
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
