using IFY.Phorm.Data;
using IFY.Phorm.EventArgs;
using IFY.Phorm.Execution;

namespace IFY.Phorm;

/// <summary>
/// Represents a session for executing Phorm contracts and database operations, providing methods for invoking actions,
/// retrieving data, and managing transactions within a scoped database connection.
/// </summary>
/// <remarks>The session exposes events for monitoring connection lifecycle, command execution, and contract
/// mapping issues. It supports both synchronous and asynchronous operations, as well as transaction management if
/// supported by the underlying runner. Configuration properties allow control over error handling and result size
/// strictness. Use this interface to interact with Phorm contracts and database entities in a scoped and configurable
/// manner.</remarks>
public interface IPhormConnectedSession
{
    #region Events

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
    /// Context data applied to this session.
    /// </summary>
    IDictionary<string, object?> ContextData { get; }

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

    #region Call/get from action contract

    /// <summary>
    /// Invokes the specified contract asynchronously and returns the result as an integer.
    /// </summary>
    /// <param name="contractName">The name of the contract to invoke. Cannot be null or empty.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the integer value returned by the
    /// contract.</returns>
    public Task<int> CallAsync(string contractName)
        => CallAsync(contractName, args: null, CancellationToken.None);
    /// <summary>
    /// Invokes an asynchronous call to the specified contract with the provided arguments.
    /// </summary>
    /// <param name="contractName">The name of the contract to invoke. Cannot be null or empty.</param>
    /// <param name="args">An object containing the arguments to pass to the contract. May be null if the contract does not require
    /// arguments.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the integer value returned by the
    /// contract call.</returns>
    public Task<int> CallAsync(string contractName, object? args)
        => CallAsync(contractName, args, CancellationToken.None);
    /// <summary>
    /// Invokes the specified contract asynchronously and returns the result as an integer.
    /// </summary>
    /// <param name="contractName">The name of the contract to invoke. Cannot be null or empty.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the integer value returned by the
    /// contract.</returns>
    public Task<int> CallAsync(string contractName, CancellationToken cancellationToken)
        => CallAsync(contractName, args: null, cancellationToken);
    /// <summary>
    /// Invokes an asynchronous call to the specified contract and returns the result as an integer.
    /// </summary>
    /// <param name="contractName">The name of the contract to invoke. Cannot be null or empty.</param>
    /// <param name="args">An optional argument object to pass to the contract. May be null if the contract does not require arguments.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the integer value returned by the
    /// contract.</returns>
    Task<int> CallAsync(string contractName, object? args, CancellationToken cancellationToken);

    /// <summary>
    /// Invokes the specified action contract asynchronously without any arguments and returns the result code.
    /// </summary>
    /// <typeparam name="TActionContract">The type of the action contract to invoke. Must implement <see cref="IPhormContract"/>.</typeparam>
    /// <returns>A task that represents the asynchronous operation. The task result contains the integer result code returned by
    /// the action contract.</returns>
    public Task<int> CallAsync<TActionContract>()
        where TActionContract : IPhormContract
        => CallAsync<TActionContract>(args: null, CancellationToken.None);
    /// <summary>
    /// Invokes the specified action contract asynchronously using the provided arguments.
    /// </summary>
    /// <typeparam name="TActionContract">The type of the action contract to invoke. Must implement <see cref="IPhormContract"/>.</typeparam>
    /// <param name="args">An object containing the arguments to pass to the action contract. May be <see langword="null"/> if the contract
    /// does not require arguments.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the status code returned by the
    /// action contract.</returns>
    public Task<int> CallAsync<TActionContract>(object? args)
        where TActionContract : IPhormContract
        => CallAsync<TActionContract>(args, CancellationToken.None);
    /// <summary>
    /// Invokes the specified action contract asynchronously without arguments and returns the result code.
    /// </summary>
    /// <typeparam name="TActionContract">The type of the action contract to invoke. Must implement <see cref="IPhormContract"/>.</typeparam>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the result code returned by the
    /// action contract.</returns>
    public Task<int> CallAsync<TActionContract>(CancellationToken cancellationToken)
        where TActionContract : IPhormContract
        => CallAsync<TActionContract>(args: null, cancellationToken);
    /// <summary>
    /// Invokes the specified action contract asynchronously and returns the result code.
    /// </summary>
    /// <typeparam name="TActionContract">The type of the action contract to execute. Must implement <see cref="IPhormContract"/>.</typeparam>
    /// <param name="contract">The action contract instance containing the parameters for the call. Cannot be null.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the integer code returned by the
    /// action.</returns>
    public Task<int> CallAsync<TActionContract>(TActionContract contract) // Same as "object? args = null", but allows better Intellisense
        where TActionContract : IPhormContract
        => CallAsync<TActionContract>(args: contract, CancellationToken.None);
    /// <summary>
    /// Invokes the specified action contract asynchronously and returns the result code.
    /// </summary>
    /// <typeparam name="TActionContract">The type of the action contract to execute. Must implement <see cref="IPhormContract"/>.</typeparam>
    /// <param name="contract">The action contract instance to invoke. Cannot be null.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the integer result code returned by
    /// the action contract.</returns>
    public Task<int> CallAsync<TActionContract>(TActionContract contract, CancellationToken cancellationToken) // Same as "object? args = null", but allows better Intellisense
        where TActionContract : IPhormContract
        => CallAsync<TActionContract>(args: contract, cancellationToken);
    /// <summary>
    /// Invokes an asynchronous call to the specified action contract with the provided arguments.
    /// </summary>
    /// <typeparam name="TActionContract">The type of the action contract to invoke. Must implement <see cref="IPhormContract"/>.</typeparam>
    /// <param name="args">An object containing the arguments to pass to the action contract. Can be <see langword="null"/> if the action
    /// does not require arguments.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains an integer status code returned by
    /// the action contract.</returns>
    Task<int> CallAsync<TActionContract>(object? args, CancellationToken cancellationToken)
        where TActionContract : IPhormContract;

    /// <summary>
    /// Creates a contract runner for the specified contract name.
    /// </summary>
    /// <param name="contractName">The name of the contract to instantiate. Cannot be null or empty.</param>
    /// <returns>An instance of <see cref="IPhormContractRunner"/> for the specified contract name.</returns>
    public IPhormContractRunner From(string contractName)
        => From(contractName, args: null);
    /// <summary>
    /// Creates a contract runner instance for the specified contract name and optional arguments.
    /// </summary>
    /// <param name="contractName">The name of the contract to instantiate. Cannot be null or empty.</param>
    /// <param name="args">An optional object containing initialization arguments for the contract. May be null if the contract does not
    /// require arguments.</param>
    /// <returns>An <see cref="IPhormContractRunner"/> instance configured for the specified contract and arguments.</returns>
    IPhormContractRunner From(string contractName, object? args);

    /// <summary>
    /// Creates a contract runner for the specified action contract type.
    /// </summary>
    /// <typeparam name="TActionContract">The type of action contract to execute. Must implement <see cref="IPhormContract"/>.</typeparam>
    /// <returns>An <see cref="IPhormContractRunner{TActionContract}"/> instance for executing the specified contract type.</returns>
    public IPhormContractRunner<TActionContract> From<TActionContract>()
         where TActionContract : IPhormContract
         => From<TActionContract>(args: null);
    /// <summary>
    /// Creates a contract runner for the specified action contract type, optionally initializing it with the provided
    /// arguments.
    /// </summary>
    /// <typeparam name="TActionContract">The type of action contract to be executed. Must implement <see cref="IPhormContract"/>.</typeparam>
    /// <param name="args">An optional object containing initialization arguments for the contract. May be <see langword="null"/> if no
    /// arguments are required.</param>
    /// <returns>An <see cref="IPhormContractRunner{TActionContract}"/> instance configured for the specified contract type.</returns>
    IPhormContractRunner<TActionContract> From<TActionContract>(object? args)
        where TActionContract : IPhormContract;
    /// <summary>
    /// Creates a contract runner for the specified action contract instance.
    /// </summary>
    /// <typeparam name="TActionContract">The type of the action contract to be executed. Must implement <see cref="IPhormContract"/>.</typeparam>
    /// <param name="contract">The action contract instance to be used for execution. Cannot be null.</param>
    /// <returns>An <see cref="IPhormContractRunner{TActionContract}"/> that can execute the specified contract.</returns>
    public IPhormContractRunner<TActionContract> From<TActionContract>(TActionContract contract) // Same as "object? args = null", but allows better Intellisense
        where TActionContract : IPhormContract
        => From<TActionContract>(args: contract);

    #endregion Call/get from action contract

    #region Get from Table/View

    /// <summary>
    /// Asynchronously retrieves a result of the specified reference type, or null if no result is available.
    /// </summary>
    /// <typeparam name="TResult">The type of the result to retrieve. Must be a reference type.</typeparam>
    /// <returns>A task that represents the asynchronous operation. The task result contains the retrieved object of type
    /// <typeparamref name="TResult"/>, or null if no result is found.</returns>
    public Task<TResult?> GetAsync<TResult>()
        where TResult : class
        => GetAsync<TResult>(args: null, CancellationToken.None);
    /// <summary>
    /// Asynchronously retrieves a result of the specified type using the provided arguments.
    /// </summary>
    /// <typeparam name="TResult">The type of the result to retrieve. Must be a reference type.</typeparam>
    /// <param name="args">An object containing the arguments required to perform the retrieval. Can be null if no arguments are needed.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the retrieved object of type
    /// <typeparamref name="TResult"/> if found; otherwise, null.</returns>
    public Task<TResult?> GetAsync<TResult>(object? args)
        where TResult : class
        => GetAsync<TResult>(args, CancellationToken.None);
    /// <summary>
    /// Asynchronously retrieves a result of the specified reference type, supporting cancellation.
    /// </summary>
    /// <typeparam name="TResult">The type of the result to retrieve. Must be a reference type.</typeparam>
    /// <param name="cancellationToken">A token that can be used to cancel the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the retrieved value of type
    /// <typeparamref name="TResult"/>, or <see langword="null"/> if no result is available.</returns>
    public Task<TResult?> GetAsync<TResult>(CancellationToken cancellationToken)
        where TResult : class
        => GetAsync<TResult>(args: null, cancellationToken);
    /// <summary>
    /// Asynchronously retrieves a result based on the specified contract object.
    /// </summary>
    /// <typeparam name="TResult">The type of the contract object and the result to retrieve. Must be a reference type.</typeparam>
    /// <param name="contract">The contract object that specifies the parameters for the retrieval operation. Cannot be null.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the retrieved object of type
    /// TResult, or null if no result is found.</returns>
    public Task<TResult?> GetAsync<TResult>(TResult contract) // Same as "object? args = null", but allows better Intellisense
        where TResult : class
        => GetAsync<TResult>(args: contract, CancellationToken.None);
    /// <summary>
    /// Asynchronously retrieves a result of the specified contract type.
    /// </summary>
    /// <typeparam name="TResult">The type of the contract to retrieve. Must be a reference type.</typeparam>
    /// <param name="contract">An instance of the contract type that specifies the retrieval criteria. Cannot be null.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the retrieved contract instance, or
    /// null if no matching result is found.</returns>
    public Task<TResult?> GetAsync<TResult>(TResult contract, CancellationToken cancellationToken) // Same as "object? args = null", but allows better Intellisense
        where TResult : class
        => GetAsync<TResult>(args: contract, cancellationToken);
    /// <summary>
    /// Asynchronously retrieves a result of the specified reference type using the provided arguments.
    /// </summary>
    /// <typeparam name="TResult">The type of the result to retrieve. Must be a reference type.</typeparam>
    /// <param name="args">An object containing the arguments required to perform the retrieval. May be null if no arguments are needed.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the retrieved object of type
    /// TResult, or null if no result is found.</returns>
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
    Task<ITransactedPhormSession> BeginTransactionAsync(CancellationToken cancellationToken);
    /// <summary>
    /// Begin a new transaction, with associated runner.
    /// </summary>
    /// <returns>The runner of the transaction.</returns>
    [Obsolete("Use BeginTransactionAsync")]
    ITransactedPhormSession BeginTransaction() => BeginTransactionAsync(default).Result;

    #endregion Transactions
}
