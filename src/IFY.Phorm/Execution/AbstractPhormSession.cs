using IFY.Phorm.Connectivity;
using IFY.Phorm.Data;
using IFY.Phorm.EventArgs;
using System.Data;

namespace IFY.Phorm.Execution;

/// <summary>
/// Represents a session for executing Phorm contracts and database operations, providing methods for invoking actions,
/// retrieving data, and managing transactions within a scoped database connection.
/// </summary>
/// <remarks>The session exposes events for monitoring connection lifecycle, command execution, and contract
/// mapping issues. It supports both synchronous and asynchronous operations, as well as transaction management if
/// supported by the underlying runner. Configuration properties allow control over error handling and result size
/// strictness. Use this interface to interact with Phorm contracts and database entities in a scoped and configurable
/// manner.</remarks>
public abstract class AbstractPhormSession(string databaseConnectionString, string? connectionName)
    : IPhormConnectedSession
{
    /// <summary>
    /// The connection string used to connect to the database.
    /// </summary>
    protected readonly string _databaseConnectionString = databaseConnectionString;

    /// <inheritdoc/>
    public string? ConnectionName { get; } = connectionName;
    /// <inheritdoc/>
    public IDictionary<string, object?> ContextData { get; } = new Dictionary<string, object?>();

    /// <summary>
    /// Gets or sets the prefix used for accessing database stored procedures.
    /// </summary>
    public string ProcedurePrefix
    {
        get;
#if !NET5_0_OR_GREATER
        set;
#else
        init;
#endif
    } = GlobalSettings.ProcedurePrefix;
    /// <summary>
    /// Gets or sets the prefix used for accessing database tables.
    /// </summary>
    public string TablePrefix
    {
        get;
#if !NET5_0_OR_GREATER
        set;
#else
        init;
#endif
    } = GlobalSettings.TablePrefix;
    /// <summary>
    /// Gets or sets the prefix used for accessing database views.
    /// </summary>
    public string ViewPrefix
    {
        get;
#if !NET5_0_OR_GREATER
        set;
#else
        init;
#endif
    } = GlobalSettings.ViewPrefix;

    #region Events

    /// <inheritdoc/>
    public event EventHandler<ConnectedEventArgs>? Connected;
    internal void OnConnected(ConnectedEventArgs args)
    {
        try { Connected?.Invoke(this, args); } catch { /* Consume handler errors */ }
        Events.OnConnected(this, args);
    }

    /// <inheritdoc/>
    public event EventHandler<CommandExecutingEventArgs>? CommandExecuting;
    internal void OnCommandExecuting(CommandExecutingEventArgs args)
    {
        try { CommandExecuting?.Invoke(this, args); } catch { /* Consume handler errors */ }
        Events.OnCommandExecuting(this, args);
    }

    /// <inheritdoc/>
    public event EventHandler<CommandExecutedEventArgs>? CommandExecuted;
    internal void OnCommandExecuted(CommandExecutedEventArgs args)
    {
        try { CommandExecuted?.Invoke(this, args); } catch { /* Consume handler errors */ }
        Events.OnCommandExecuted(this, args);
    }

    /// <inheritdoc/>
    public event EventHandler<UnexpectedRecordColumnEventArgs>? UnexpectedRecordColumn;
    internal void OnUnexpectedRecordColumn(UnexpectedRecordColumnEventArgs args)
    {
        try { UnexpectedRecordColumn?.Invoke(this, args); } catch { /* Consume handler errors */ }
        Events.OnUnexpectedRecordColumn(this, args);
    }

    /// <inheritdoc/>
    public event EventHandler<UnresolvedContractMemberEventArgs>? UnresolvedContractMember;
    internal void OnUnresolvedContractMember(UnresolvedContractMemberEventArgs args)
    {
        try { UnresolvedContractMember?.Invoke(this, args); } catch { /* Consume handler errors */ }
        Events.OnUnresolvedContractMember(this, args);
    }

    /// <inheritdoc/>
    public event EventHandler<ConsoleMessageEventArgs>? ConsoleMessage;
    internal void OnConsoleMessage(ConsoleMessageEventArgs args)
    {
        try { ConsoleMessage?.Invoke(this, args); } catch { /* Consume handler errors */ }
        Events.OnConsoleMessage(this, args);
    }

    #endregion Events

    /// <inheritdoc/>
    public bool ExceptionsAsConsoleMessage { get; set; } = GlobalSettings.ExceptionsAsConsoleMessage;

    /// <inheritdoc/>
    public bool StrictResultSize { get; set; } = GlobalSettings.StrictResultSize;

    #region Connection

    /// <summary>
    /// Creates and returns a prepared database connection for the current context.
    /// </summary>
    /// <param name="readOnly">true to request a connection optimised for read-only operations; otherwise, false to request a connection that
    /// supports write operations.</param>
    /// <returns>An <see cref="IPhormDbConnection"/> instance representing the database connection for the current context.</returns>
    protected internal virtual IPhormDbConnection GetConnection(bool readOnly)
    {
        // Reuse existing Phorm connections, where possible
        var connStr = GetConnectionString(readOnly);
        var conn = CreateConnection(connStr);
        return new PhormDbConnection(this, conn);
    }

    /// <summary>
    /// Retrieves the database connection string for the specified access mode.
    /// </summary>
    /// <param name="readOnly">true to retrieve a connection string configured for read-only access; false to retrieve a connection string for
    /// read-write access.</param>
    /// <returns>A connection string that can be used to connect to the database with the requested access mode.</returns>
    protected abstract string GetConnectionString(bool readOnly);

    /// <summary>
    /// Creates and returns a new database connection configured for either read-only or read-write access.
    /// </summary>
    /// <param name="connectionString">The connection string to use for creating the database connection.</param>
    /// <returns>An <see cref="IDbConnection"/> instance representing the newly created database connection.</returns>
    protected abstract IAsyncDbConnection CreateConnection(string connectionString);

    /// <summary>
    /// Asynchronously applies the current context to the specified database connection.
    /// </summary>
    /// <param name="phormConn">The database connection to which the context will be applied. Cannot be null.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    protected internal virtual Task ApplyContextAsync(IPhormDbConnection phormConn) => Task.CompletedTask;

    /// <summary>
    /// Implementations to provide logic for resolving the default schema of the connection.
    /// </summary>
    /// <returns>The default schema name, if known.</returns>
    protected internal virtual Task ResolveDefaultSchemaAsync(IPhormDbConnection phormConn) => Task.CompletedTask;

    #endregion Connection

    /// <summary>
    /// Asynchronously creates an open database command for the specified object and schema.
    /// </summary>
    /// <param name="schema">The name of the database schema containing the object, or null to use the connection's default schema. If an
    /// empty string is provided, the default schema is used.</param>
    /// <param name="objectName">The name of the database object for which to create the command. Cannot be null or empty.</param>
    /// <param name="objectType">The type of the database object, such as table, view, or stored procedure.</param>
    /// <param name="readOnly">true to create the command using a read-only connection; otherwise, false.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains an <see cref="IAsyncDbCommand"/> for
    /// the specified database object.</returns>
    internal async Task<IAsyncDbCommand> CreateCommandAsync(string? schema, string objectName, DbObjectType objectType, bool readOnly)
    {
        var conn = GetConnection(readOnly);
        await conn.OpenAsync(default);
        schema = schema?.Length > 0 ? schema : conn.DefaultSchema;
        return CreateCommand(conn, schema, objectName, objectType);
    }

    /// <inheritdoc/>
    protected virtual IAsyncDbCommand CreateCommand(IPhormDbConnection connection, string schema, string objectName, DbObjectType objectType)
    {
        // Complete object name
        objectName = objectType switch
        {
            DbObjectType.StoredProcedure => objectName.FirstOrDefault() == '#'
                ? objectName : ProcedurePrefix + objectName, // Support temp sprocs
            DbObjectType.View => ViewPrefix + objectName,
            DbObjectType.Table => TablePrefix + objectName,
            _ => throw new NotSupportedException($"Unsupported object type: {objectType}")
        };

        var cmd = connection.CreateCommand();

        if (objectType is DbObjectType.Table or DbObjectType.View)
        {
            cmd.CommandType = CommandType.Text;
            // TODO: Could replace '*' with desired column names, validated by cached SchemaOnly call
            // TODO: Can do TOP 2 if we know single entity Get, to know only 1 item
            cmd.CommandText = $"SELECT * FROM [{schema}].[{objectName}]";
            return cmd;
        }

        cmd.CommandType = CommandType.StoredProcedure;
        cmd.CommandText = $"[{schema}].[{objectName}]";
        return cmd;
    }

    #region Console capture

    /// <summary>
    /// If the connection implementation supports capture of console output (print statements),
    /// this method returns a new <see cref="AbstractConsoleMessageCapture"/> that will receive the output.
    /// </summary>
    /// <param name="commandGuid">Unique id of the command being captured.</param>
    /// <param name="cmd">The command to capture console output for.</param>
    /// <returns>The object that will be provide the final console output.</returns>
    protected internal virtual AbstractConsoleMessageCapture StartConsoleCapture(Guid commandGuid, IAsyncDbCommand cmd)
        => NullConsoleMessageCapture.Instance;

    /// <summary>
    /// Provides a no-op implementation of <see cref="AbstractConsoleMessageCapture"/> that does not capture or process
    /// console messages or exceptions.
    /// </summary>
    protected internal class NullConsoleMessageCapture : AbstractConsoleMessageCapture
    {
        /// <summary>
        /// Represents a singleton instance of a console message capture that performs no operations.
        /// </summary>
        public static readonly NullConsoleMessageCapture Instance = new();
        private NullConsoleMessageCapture() : base(null!, Guid.Empty) { }
        /// <inheritdoc/>
        public override bool ProcessException(Exception ex) => false;
        /// <inheritdoc/>
        public override void Dispose() { /* Nothing to release */ }
    }

    #endregion Console capture

    #region Call

    /// <inheritdoc/>
    public Task<int> CallAsync(string contractName, object? args, CancellationToken cancellationToken)
    {
        var runner = new PhormContractRunner<IPhormContract>(this, contractName, DbObjectType.StoredProcedure, args, null);
        return runner.CallAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public Task<int> CallAsync<TActionContract>(object? args, CancellationToken cancellationToken)
        where TActionContract : IPhormContract
    {
        var runner = new PhormContractRunner<TActionContract>(this, null, DbObjectType.StoredProcedure, args, null);
        return runner.CallAsync(cancellationToken);
    }

    #endregion Call

    #region From

    /// <inheritdoc/>
    public IPhormContractRunner From(string contractName, object? args)
    {
        return new PhormContractRunner<IPhormContract>(this, contractName, DbObjectType.StoredProcedure, args, null);
    }

    /// <inheritdoc/>
    public IPhormContractRunner<TActionContract> From<TActionContract>(object? args)
        where TActionContract : IPhormContract
    {
        return new PhormContractRunner<TActionContract>(this, null, DbObjectType.StoredProcedure, args, null);
    }

    #endregion From

    #region Get

    /// <inheritdoc/>
    public Task<TResult?> GetAsync<TResult>(object? args, CancellationToken cancellationToken)
        where TResult : class
    {
        var runner = new PhormContractRunner<IPhormContract>(this, typeof(TResult), null, DbObjectType.View, args, null);
        return runner.GetAsync<TResult>(cancellationToken);
    }

    #endregion Get

    #region Transactions

    /// <inheritdoc/>
    public abstract bool SupportsTransactions { get; }

    /// <inheritdoc/>
    public abstract bool IsInTransaction { get; }

    /// <inheritdoc/>
    public abstract Task<ITransactedPhormSession> BeginTransactionAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Wraps the current session with transactional support using the specified database transaction.
    /// </summary>
    /// <param name="transaction">The database transaction to associate with the session. Cannot be null.</param>
    /// <returns>An instance of <see cref="ITransactedPhormSession"/> that operates within the context of the provided
    /// transaction.</returns>
    protected ITransactedPhormSession WrapSessionAsTransacted(IDbTransaction transaction)
    {
        return new TransactedPhormSession(this, transaction);
    }

    #endregion Transactions

#if NETSTANDARD
    // These should not be necessary, but .NET Core 3.1 is failing at runtime without them

    /// <inheritdoc/>
    public Task<int> CallAsync<TActionContract>()
        where TActionContract : IPhormContract
        => CallAsync<TActionContract>(args: null, CancellationToken.None);
    /// <inheritdoc/>
    public Task<int> CallAsync<TActionContract>(object? args)
        where TActionContract : IPhormContract
        => CallAsync<TActionContract>(args, CancellationToken.None);
    /// <inheritdoc/>
    public Task<int> CallAsync<TActionContract>(TActionContract contract)
        where TActionContract : IPhormContract
        => CallAsync<TActionContract>(args: contract, CancellationToken.None);
    /// <inheritdoc/>
    public Task<int> CallAsync<TActionContract>(TActionContract contract, CancellationToken cancellationToken) // Same as "object? args = null", but allows better Intellisense
        where TActionContract : IPhormContract
        => CallAsync<TActionContract>(args: contract, cancellationToken);

    /// <inheritdoc/>
    public Task<TResult?> GetAsync<TResult>()
        where TResult : class
        => GetAsync<TResult>(args: null, CancellationToken.None);
    /// <inheritdoc/>
    public Task<TResult?> GetAsync<TResult>(object? args)
        where TResult : class
        => GetAsync<TResult>(args, CancellationToken.None);
    /// <inheritdoc/>
    public Task<TResult?> GetAsync<TResult>(CancellationToken cancellationToken)
        where TResult : class
        => GetAsync<TResult>(args: null, cancellationToken);
    /// <inheritdoc/>
    public Task<TResult?> GetAsync<TResult>(TResult contract) // Same as "object? args = null", but allows better Intellisense
        where TResult : class
        => GetAsync<TResult>(args: contract, CancellationToken.None);
    /// <inheritdoc/>
    public Task<TResult?> GetAsync<TResult>(TResult contract, CancellationToken cancellationToken) // Same as "object? args = null", but allows better Intellisense
        where TResult : class
        => GetAsync<TResult>(args: contract, cancellationToken);
#endif
}
